// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal partial class DocumentState
{
    private static readonly LoadTextOptions s_loadTextOptions = new(SourceHashAlgorithm.Sha256);

    private static readonly TextAndVersion s_emptyTextAndVersion = TextAndVersion.Create(
        SourceText.From(string.Empty),
        VersionStamp.Default);

    public static readonly TextLoader EmptyLoader = TextLoader.From(s_emptyTextAndVersion);

    public HostDocument HostDocument { get; }
    public int Version { get; }

    private TextAndVersion? _textAndVersion;
    private readonly TextLoader _textLoader;

    private ComputedStateTracker? _computedState;

    private DocumentState(
        HostDocument hostDocument,
        int version,
        TextAndVersion? textAndVersion,
        TextLoader? textLoader)
    {
        HostDocument = hostDocument;
        Version = version;
        _textAndVersion = textAndVersion;
        _textLoader = textLoader ?? EmptyLoader;
    }

    // Internal for testing
    internal DocumentState(HostDocument hostDocument, int version, SourceText text, VersionStamp textVersion)
        : this(hostDocument, version, TextAndVersion.Create(text, textVersion), textLoader: null)
    {
    }

    // Internal for testing
    internal DocumentState(HostDocument hostDocument, int version, TextLoader loader)
        : this(hostDocument, version, textAndVersion: null, loader)
    {
    }

    public static DocumentState Create(HostDocument hostDocument, int version, TextLoader loader)
    {
        return new DocumentState(hostDocument, version, loader);
    }

    public static DocumentState Create(HostDocument hostDocument, TextLoader loader)
    {
        return new DocumentState(hostDocument, version: 1, loader);
    }

    public bool IsGeneratedOutputResultAvailable => ComputedState.IsResultAvailable;

    private ComputedStateTracker ComputedState
        => _computedState ??= InterlockedOperations.Initialize(ref _computedState, new ComputedStateTracker());

    public bool TryGetGeneratedOutputAndVersion(out (RazorCodeDocument output, VersionStamp inputVersion) result)
    {
        return ComputedState.TryGetGeneratedOutputAndVersion(out result);
    }

    public Task<(RazorCodeDocument output, VersionStamp inputVersion)> GetGeneratedOutputAndVersionAsync(
        ProjectSnapshot project,
        DocumentSnapshot document,
        CancellationToken cancellationToken)
    {
        return ComputedState.GetGeneratedOutputAndVersionAsync(project, document, cancellationToken);
    }

    public ValueTask<TextAndVersion> GetTextAndVersionAsync(CancellationToken cancellationToken)
    {
        return _textAndVersion is TextAndVersion result
            ? new(result)
            : LoadTextAndVersionAsync(_textLoader, cancellationToken);

        async ValueTask<TextAndVersion> LoadTextAndVersionAsync(TextLoader loader, CancellationToken cancellationToken)
        {
            var textAndVersion = await loader
                .LoadTextAndVersionAsync(s_loadTextOptions, cancellationToken)
                .ConfigureAwait(false);

            return InterlockedOperations.Initialize(ref _textAndVersion, textAndVersion);
        }
    }

    public ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
    {
        return TryGetText(out var text)
            ? new(text)
            : GetTextCoreAsync(cancellationToken);

        async ValueTask<SourceText> GetTextCoreAsync(CancellationToken cancellationToken)
        {
            var textAsVersion = await GetTextAndVersionAsync(cancellationToken).ConfigureAwait(false);

            return textAsVersion.Text;
        }
    }

    public ValueTask<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken)
    {
        return TryGetTextVersion(out var version)
            ? new(version)
            : GetTextVersionCoreAsync(cancellationToken);

        async ValueTask<VersionStamp> GetTextVersionCoreAsync(CancellationToken cancellationToken)
        {
            var textAsVersion = await GetTextAndVersionAsync(cancellationToken).ConfigureAwait(false);

            return textAsVersion.Version;
        }
    }

    public bool TryGetText([NotNullWhen(true)] out SourceText? result)
    {
        if (_textAndVersion is { } textAndVersion)
        {
            result = textAndVersion.Text;
            return true;
        }

        result = null;
        return false;
    }

    public bool TryGetTextVersion(out VersionStamp result)
    {
        if (_textAndVersion is { } textAndVersion)
        {
            result = textAndVersion.Version;
            return true;
        }

        result = default;
        return false;
    }

    public virtual DocumentState WithConfigurationChange()
    {
        var state = new DocumentState(HostDocument, Version + 1, _textAndVersion, _textLoader);

        // Do not cache computed state

        return state;
    }

    public virtual DocumentState WithImportsChange()
    {
        var state = new DocumentState(HostDocument, Version + 1, _textAndVersion, _textLoader);

        // Optimistically cache the computed state
        state._computedState = new ComputedStateTracker(_computedState);

        return state;
    }

    public virtual DocumentState WithProjectWorkspaceStateChange()
    {
        var state = new DocumentState(HostDocument, Version + 1, _textAndVersion, _textLoader);

        // Optimistically cache the computed state
        state._computedState = new ComputedStateTracker(_computedState);

        return state;
    }

    public virtual DocumentState WithText(SourceText text, VersionStamp textVersion)
    {
        // Do not cache the computed state

        return new DocumentState(HostDocument, Version + 1, TextAndVersion.Create(text, textVersion), textLoader: null);
    }

    public virtual DocumentState WithTextLoader(TextLoader textLoader)
    {
        // Do not cache the computed state

        return new DocumentState(HostDocument, Version + 1, textAndVersion: null, textLoader);
    }

    internal static async Task<RazorCodeDocument> GenerateCodeDocumentAsync(
        IDocumentSnapshot document,
        RazorProjectEngine projectEngine,
        ImmutableArray<ImportItem> importItems,
        ImmutableArray<TagHelperDescriptor> tagHelpers,
        bool forceRuntimeCodeGeneration,
        CancellationToken cancellationToken)
    {
        using var importSources = new PooledArrayBuilder<RazorSourceDocument>(importItems.Length);
        foreach (var importItem in importItems)
        {
            var sourceDocument = await importItem.GetSourceDocumentAsync(cancellationToken).ConfigureAwait(false);
            importSources.Add(sourceDocument);
        }

        var filePath = document.FilePath.AssumeNotNull();
        var fileKind = document.FileKind.AssumeNotNull();
        var projectItem = projectEngine.FileSystem.GetItem(filePath, fileKind);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var source = RazorSourceDocument.Create(text, RazorSourceDocumentProperties.Create(document.FilePath, projectItem?.RelativePhysicalPath));

        if (forceRuntimeCodeGeneration)
        {
            return projectEngine.Process(source, fileKind, importSources.DrainToImmutable(), tagHelpers);
        }

        return projectEngine.ProcessDesignTime(source, fileKind, importSources.DrainToImmutable(), tagHelpers);
    }

    internal static ImmutableArray<ImportItem> GetImports(IDocumentSnapshot document, RazorProjectEngine projectEngine)
    {
        var project = document.Project;
        var filePath = document.FilePath.AssumeNotNull();
        var fileKind = document.FileKind.AssumeNotNull();

        var documentProjectItem = projectEngine.FileSystem.GetItem(filePath, fileKind);

        using var importProjectItems = new PooledArrayBuilder<RazorProjectItem>();

        foreach (var feature in projectEngine.ProjectFeatures.OfType<IImportProjectFeature>())
        {
            if (feature.GetImports(documentProjectItem) is { } featureImports)
            {
                importProjectItems.AddRange(featureImports);
            }
        }

        if (importProjectItems.Count == 0)
        {
            return [];
        }

        using var imports = new PooledArrayBuilder<ImportItem>(capacity: importProjectItems.Count);

        foreach (var importProjectItem in importProjectItems)
        {
            if (importProjectItem is NotFoundProjectItem)
            {
                continue;
            }

            if (importProjectItem.PhysicalPath is null)
            {
                // This is a default import.
                imports.Add(ImportItem.CreateDefaultImport(importProjectItem));
            }
            else if (project.TryGetDocument(importProjectItem.PhysicalPath, out var importDocument))
            {
                imports.Add(ImportItem.CreateDocumentImport(importDocument, importProjectItem));
            }
        }

        return imports.DrainToImmutable();
    }
}
