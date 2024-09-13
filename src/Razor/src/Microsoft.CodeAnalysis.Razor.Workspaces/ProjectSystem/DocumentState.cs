// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal partial class DocumentState
{
    private static readonly TextAndVersion s_emptyText = TextAndVersion.Create(SourceText.From(string.Empty), VersionStamp.Default);

    public static readonly Func<Task<TextAndVersion>> EmptyLoader = () => Task.FromResult(s_emptyText);

    public HostDocument HostDocument { get; }

    private readonly int _version;
    public int Version => _version;

    private readonly object _lock = new();

    private ComputedStateTracker? _computedState;

    private readonly Func<Task<TextAndVersion>> _loader;
    private Task<TextAndVersion>? _loaderTask;
    private TextAndVersion? _textAndVersion;

    public static DocumentState Create(HostDocument hostDocument, Func<Task<TextAndVersion>> loader)
        => new(hostDocument, version: 1, loader);

    public static DocumentState Create(HostDocument hostDocument)
        => new(hostDocument, version: 1, EmptyLoader);

    public static DocumentState Create(HostDocument hostDocument, int version, TextAndVersion textAndVersion)
        => new(hostDocument, version, textAndVersion);

    public static DocumentState Create(HostDocument hostDocument, TextAndVersion textAndVersion)
        => new(hostDocument, version: 1, textAndVersion);

    protected DocumentState(HostDocument hostDocument, int version, TextAndVersion textAndVersion)
    {
        HostDocument = hostDocument;
        _version = version;
        _textAndVersion = textAndVersion;
        _loader = EmptyLoader;
    }

    protected DocumentState(HostDocument hostDocument, int version, Func<Task<TextAndVersion>> loader)
    {
        HostDocument = hostDocument;
        _version = version;
        _loader = loader;
    }

    private DocumentState(
        HostDocument hostDocument,
        int version,
        TextAndVersion? textAndVersion,
        Func<Task<TextAndVersion>> loader)
    {
        HostDocument = hostDocument;
        _textAndVersion = textAndVersion;
        _version = version;
        _loader = loader;
    }

    public bool IsGeneratedOutputResultAvailable => ComputedState.IsResultAvailable == true;

    private ComputedStateTracker ComputedState
    {
        get
        {
            if (_computedState is null)
            {
                lock (_lock)
                {
                    _computedState ??= new ComputedStateTracker(this);
                }
            }

            return _computedState;
        }
    }

    public Task<(RazorCodeDocument output, VersionStamp inputVersion)> GetGeneratedOutputAndVersionAsync(ProjectSnapshot project, DocumentSnapshot document)
    {
        return ComputedState.GetGeneratedOutputAndVersionAsync(project, document);
    }

    public async Task<SourceText> GetTextAsync()
    {
        if (TryGetText(out var text))
        {
            return text;
        }

        lock (_lock)
        {
            _loaderTask = _loader();
        }

        return (await _loaderTask.ConfigureAwait(false)).Text;
    }

    public async Task<VersionStamp> GetTextVersionAsync()
    {
        if (TryGetTextVersion(out var version))
        {
            return version;
        }

        lock (_lock)
        {
            _loaderTask = _loader();
        }

        return (await _loaderTask.ConfigureAwait(false)).Version;
    }

    public bool TryGetText([NotNullWhen(true)] out SourceText? result)
    {
        if (_textAndVersion is TextAndVersion textAndVersion)
        {
            result = textAndVersion.Text;
            return true;
        }

        if (_loaderTask is { } loaderTask && loaderTask.IsCompleted)
        {
            result = loaderTask.VerifyCompleted().Text;
            return true;
        }

        result = null;
        return false;
    }

    public bool TryGetTextVersion(out VersionStamp result)
    {
        if (_textAndVersion is TextAndVersion textAndVersion)
        {
            result = textAndVersion.Version;
            return true;
        }

        if (_loaderTask is { } loaderTask && loaderTask.IsCompleted)
        {
            result = loaderTask.VerifyCompleted().Version;
            return true;
        }

        result = default;
        return false;
    }

    public virtual DocumentState WithConfigurationChange()
    {
        var state = new DocumentState(HostDocument, _version + 1, _textAndVersion, _loader)
        {
            // The source could not have possibly changed.
            _textAndVersion = _textAndVersion,
            _loaderTask = _loaderTask,
        };

        // Do not cache computed state

        return state;
    }

    public virtual DocumentState WithImportsChange()
    {
        var state = new DocumentState(HostDocument, _version + 1, _textAndVersion, _loader)
        {
            // The source could not have possibly changed.
            _textAndVersion = _textAndVersion,
            _loaderTask = _loaderTask
        };

        // Optimistically cache the computed state
        state._computedState = new ComputedStateTracker(state, _computedState);

        return state;
    }

    public virtual DocumentState WithProjectWorkspaceStateChange()
    {
        var state = new DocumentState(HostDocument, _version + 1, _textAndVersion, _loader)
        {
            // The source could not have possibly changed.
            _textAndVersion = _textAndVersion,
            _loaderTask = _loaderTask
        };

        // Optimistically cache the computed state
        state._computedState = new ComputedStateTracker(state, _computedState);

        return state;
    }

    public virtual DocumentState WithText(SourceText sourceText, VersionStamp textVersion)
    {
        // Do not cache the computed state

        return new DocumentState(HostDocument, _version + 1, TextAndVersion.Create(sourceText, textVersion));
    }

    public virtual DocumentState WithTextLoader(Func<Task<TextAndVersion>> loader)
    {
        // Do not cache the computed state

        return new DocumentState(HostDocument, _version + 1, loader);
    }

    // Internal, because we are temporarily sharing code with CohostDocumentSnapshot
    internal static ImmutableArray<IDocumentSnapshot> GetImportsCore(IProjectSnapshot project, RazorProjectEngine projectEngine, string filePath, string fileKind)
    {
        var projectItem = projectEngine.FileSystem.GetItem(filePath, fileKind);

        using var _1 = ListPool<RazorProjectItem>.GetPooledObject(out var importItems);

        foreach (var feature in projectEngine.ProjectFeatures.OfType<IImportProjectFeature>())
        {
            if (feature.GetImports(projectItem) is { } featureImports)
            {
                importItems.AddRange(featureImports);
            }
        }

        if (importItems.Count == 0)
        {
            return [];
        }

        using var _2 = ArrayBuilderPool<IDocumentSnapshot>.GetPooledObject(out var imports);

        foreach (var item in importItems)
        {
            if (item is NotFoundProjectItem)
            {
                continue;
            }

            if (item.PhysicalPath is null)
            {
                // This is a default import.
                var defaultImport = new ImportDocumentSnapshot(project, item);
                imports.Add(defaultImport);
            }
            else if (project.GetDocument(item.PhysicalPath) is { } import)
            {
                imports.Add(import);
            }
        }

        return imports.ToImmutable();
    }

    internal static async Task<RazorCodeDocument> GenerateCodeDocumentAsync(IDocumentSnapshot document, RazorProjectEngine projectEngine, ImmutableArray<ImportItem> imports, ImmutableArray<TagHelperDescriptor> tagHelpers, bool forceRuntimeCodeGeneration)
    {
        // OK we have to generate the code.
        using var importSources = new PooledArrayBuilder<RazorSourceDocument>(imports.Length);
        foreach (var item in imports)
        {
            var importProjectItem = item.FilePath is null ? null : projectEngine.FileSystem.GetItem(item.FilePath, item.FileKind);
            var sourceDocument = await GetRazorSourceDocumentAsync(item.Document, importProjectItem).ConfigureAwait(false);
            importSources.Add(sourceDocument);
        }

        var projectItem = document.FilePath is null ? null : projectEngine.FileSystem.GetItem(document.FilePath, document.FileKind);
        var documentSource = await GetRazorSourceDocumentAsync(document, projectItem).ConfigureAwait(false);

        if (forceRuntimeCodeGeneration)
        {
            return projectEngine.Process(documentSource, fileKind: document.FileKind, importSources.DrainToImmutable(), tagHelpers);
        }

        return projectEngine.ProcessDesignTime(documentSource, fileKind: document.FileKind, importSources.DrainToImmutable(), tagHelpers);
    }

    internal static async Task<ImmutableArray<ImportItem>> GetImportsAsync(IDocumentSnapshot document, RazorProjectEngine projectEngine)
    {
        var imports = GetImportsCore(document.Project, projectEngine, document.FilePath.AssumeNotNull(), document.FileKind.AssumeNotNull());
        using var result = new PooledArrayBuilder<ImportItem>(imports.Length);

        foreach (var snapshot in imports)
        {
            var versionStamp = await snapshot.GetTextVersionAsync().ConfigureAwait(false);
            result.Add(new ImportItem(snapshot.FilePath, versionStamp, snapshot));
        }

        return result.DrainToImmutable();
    }

    private static async Task<RazorSourceDocument> GetRazorSourceDocumentAsync(IDocumentSnapshot document, RazorProjectItem? projectItem)
    {
        var sourceText = await document.GetTextAsync().ConfigureAwait(false);
        return RazorSourceDocument.Create(sourceText, RazorSourceDocumentProperties.Create(document.FilePath, projectItem?.RelativePhysicalPath));
    }
}
