// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal partial class DocumentState
{
    private readonly object _lock = new();

    private ComputedStateTracker? _computedState;

    private readonly RazorTextLoader _loader;
    private Task<TextAndVersion>? _loaderTask;
    private SourceText? _sourceText;
    private VersionStamp? _version;

    public static DocumentState Create(HostDocument hostDocument, RazorTextLoader? loader = null)
        => new(hostDocument, text: null, version: null, loader);

    // Internal for testing
    internal DocumentState(HostDocument hostDocument, SourceText? text, VersionStamp? version, RazorTextLoader? loader)
    {
        HostDocument = hostDocument;
        _sourceText = text;
        _version = version;
        _loader = loader ?? RazorTextLoader.Empty;
    }

    public HostDocument HostDocument { get; }

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
            _loaderTask = _loader.LoadTextAndVersionAsync(CancellationToken.None);
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
            _loaderTask = _loader.LoadTextAndVersionAsync(CancellationToken.None);
        }

        return (await _loaderTask.ConfigureAwait(false)).Version;
    }

    public bool TryGetText([NotNullWhen(true)] out SourceText? result)
    {
        if (_sourceText is { } sourceText)
        {
            result = sourceText;
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
        if (_version is { } version)
        {
            result = version;
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
        var state = new DocumentState(HostDocument, _sourceText, _version, _loader)
        {
            // The source could not have possibly changed.
            _sourceText = _sourceText,
            _version = _version,
            _loaderTask = _loaderTask
        };

        // Do not cache computed state

        return state;
    }

    public virtual DocumentState WithImportsChange()
    {
        var state = new DocumentState(HostDocument, _sourceText, _version, _loader)
        {
            // The source could not have possibly changed.
            _sourceText = _sourceText,
            _version = _version,
            _loaderTask = _loaderTask
        };

        // Optimistically cache the computed state
        state._computedState = new ComputedStateTracker(state, _computedState);

        return state;
    }

    public virtual DocumentState WithProjectWorkspaceStateChange()
    {
        var state = new DocumentState(HostDocument, _sourceText, _version, _loader)
        {
            // The source could not have possibly changed.
            _sourceText = _sourceText,
            _version = _version,
            _loaderTask = _loaderTask
        };

        // Optimistically cache the computed state
        state._computedState = new ComputedStateTracker(state, _computedState);

        return state;
    }

    public virtual DocumentState WithText(SourceText sourceText, VersionStamp version)
    {
        // Do not cache the computed state

        return new DocumentState(HostDocument, sourceText, version, loader: null);
    }

    public virtual DocumentState WithTextLoader(RazorTextLoader loader)
    {
        // Do not cache the computed state

        return new DocumentState(HostDocument, text: null, version: null, loader);
    }

    // Internal, because we are temporarily sharing code with CohostDocumentSnapshot
    internal static ImmutableArray<IDocumentSnapshot> GetImportsCore(
        IProjectSnapshot project,
        RazorProjectEngine projectEngine,
        string filePath,
        string fileKind)
    {
        var projectItem = projectEngine.FileSystem.GetItem(filePath, fileKind);

        using var importItems = new PooledArrayBuilder<RazorProjectItem>();

        foreach (var projectFeature in projectEngine.ProjectFeatures)
        {
            if (projectFeature is not IImportProjectFeature importFeature)
            {
                continue;
            }

            if (importFeature.GetImports(projectItem) is { } featureImports)
            {
                importItems.AddRange(featureImports);
            }
        }

        if (importItems.Count == 0)
        {
            return [];
        }

        using var imports = new PooledArrayBuilder<IDocumentSnapshot>(importItems.Count);

        foreach (var importItem in importItems)
        {
            if (importItem is NotFoundProjectItem)
            {
                continue;
            }

            if (importItem.PhysicalPath is null)
            {
                // This is a default import.
                imports.Add(new ImportDocumentSnapshot(project, importItem));
            }
            else if (project.TryGetDocument(importItem.PhysicalPath, out var importDocument))
            {
                imports.Add(importDocument);
            }
        }

        return imports.DrainToImmutable();
    }

    internal static async Task<RazorCodeDocument> GenerateCodeDocumentAsync(
        IDocumentSnapshot document,
        RazorProjectEngine projectEngine,
        ImmutableArray<ImportItem> imports,
        ImmutableArray<TagHelperDescriptor> tagHelpers,
        bool forceRuntimeCodeGeneration)
    {
        // OK we have to generate the code.
        using var importSources = new PooledArrayBuilder<RazorSourceDocument>(imports.Length);

        foreach (var item in imports)
        {
            var importProjectItem = GetProjectItem(item.FilePath, item.FileKind, projectEngine.FileSystem);
            var importSourceDocument = await GetRazorSourceDocumentAsync(item.Document, importProjectItem).ConfigureAwait(false);
            importSources.Add(importSourceDocument);
        }

        var projectItem = GetProjectItem(document.FilePath, document.FileKind, projectEngine.FileSystem);
        var sourceDocument = await GetRazorSourceDocumentAsync(document, projectItem).ConfigureAwait(false);

        return forceRuntimeCodeGeneration
            ? projectEngine.Process(sourceDocument, document.FileKind, importSources.DrainToImmutable(), tagHelpers)
            : projectEngine.ProcessDesignTime(sourceDocument, document.FileKind, importSources.DrainToImmutable(), tagHelpers);

        static RazorProjectItem? GetProjectItem(string? filePath, string? fileKind, RazorProjectFileSystem fileSystem)
        {
            return filePath is not null
                ? fileSystem.GetItem(filePath, fileKind)
                : null;
        }
    }

    internal static async Task<ImmutableArray<ImportItem>> GetImportsAsync(IDocumentSnapshot document, RazorProjectEngine projectEngine)
    {
        var importSnapshots = GetImportsCore(document.Project, projectEngine, document.FilePath.AssumeNotNull(), document.FileKind.AssumeNotNull());

        using var result = new PooledArrayBuilder<ImportItem>(importSnapshots.Length);

        foreach (var snapshot in importSnapshots)
        {
            var version = await snapshot.GetTextVersionAsync().ConfigureAwait(false);
            result.Add(new ImportItem(snapshot.FilePath, version, snapshot));
        }

        return result.DrainToImmutable();
    }

    private static async Task<RazorSourceDocument> GetRazorSourceDocumentAsync(IDocumentSnapshot document, RazorProjectItem? projectItem)
    {
        var sourceText = await document.GetTextAsync().ConfigureAwait(false);
        return RazorSourceDocument.Create(
            sourceText,
            RazorSourceDocumentProperties.Create(document.FilePath, projectItem?.RelativePhysicalPath));
    }
}
