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
    public HostDocument HostDocument { get; }
    public int Version { get; }

    private TextAndVersion? _textAndVersion;
    private readonly TextLoader _textLoader;

    private ComputedStateTracker? _computedState;

    private DocumentState(
        HostDocument hostDocument,
        TextAndVersion? textAndVersion,
        TextLoader? textLoader)
    {
        HostDocument = hostDocument;
        Version = 1;
        _textAndVersion = textAndVersion;
        _textLoader = textLoader ?? EmptyTextLoader.Instance;
    }

    private DocumentState(
        DocumentState other,
        TextAndVersion? textAndVersion,
        TextLoader? textLoader,
        ComputedStateTracker? computedState)
    {
        HostDocument = other.HostDocument;
        Version = other.Version + 1;
        _textAndVersion = textAndVersion;
        _textLoader = textLoader ?? EmptyTextLoader.Instance;
        _computedState = computedState;
    }

    // Private protected for testing
    private protected DocumentState(HostDocument hostDocument, TextLoader? textLoader)
        : this(hostDocument, textAndVersion: null, textLoader)
    {
    }

    public static DocumentState Create(HostDocument hostDocument, TextAndVersion textAndVersion)
        => new(hostDocument, textAndVersion, textLoader: null);

    public static DocumentState Create(HostDocument hostDocument, TextLoader textLoader)
        => new(hostDocument, textAndVersion: null, textLoader);

    public static DocumentState Create(HostDocument hostDocument)
        => new(hostDocument, textAndVersion: null, textLoader: null);

    private ComputedStateTracker ComputedState
        => _computedState ??= InterlockedOperations.Initialize(ref _computedState, new ComputedStateTracker());

    public bool TryGetGeneratedOutputAndVersion([NotNullWhen(true)] out OutputAndVersion? result)
        => ComputedState.TryGetGeneratedOutputAndVersion(out result);

    public Task<OutputAndVersion> GetGeneratedOutputAndVersionAsync(
        DocumentSnapshot document,
        CancellationToken cancellationToken)
    {
        return ComputedState.GetGeneratedOutputAndVersionAsync(document, cancellationToken);
    }

    public ValueTask<TextAndVersion> GetTextAndVersionAsync(CancellationToken cancellationToken)
    {
        return _textAndVersion is TextAndVersion result
            ? new(result)
            : LoadTextAndVersionAsync(_textLoader, cancellationToken);

        async ValueTask<TextAndVersion> LoadTextAndVersionAsync(TextLoader textLoader, CancellationToken cancellationToken)
        {
            var textAndVersion = await textLoader
                .LoadTextAndVersionAsync(new(SourceHashAlgorithm.Sha256), cancellationToken)
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
        return new DocumentState(this, _textAndVersion, _textLoader, computedState: null);
    }

    public virtual DocumentState WithImportsChange()
    {
        // Optimistically cache the computed state
        return new DocumentState(this, _textAndVersion, _textLoader, _computedState);
    }

    public virtual DocumentState WithProjectWorkspaceStateChange()
    {
        // Optimistically cache the computed state
        return new DocumentState(this, _textAndVersion, _textLoader, _computedState);
    }

    public DocumentState WithText(SourceText text, VersionStamp textVersion)
        => new(this, TextAndVersion.Create(text, textVersion), textLoader: null, computedState: null);

    public DocumentState WithTextLoader(TextLoader textLoader)
        => new(this, textAndVersion: null, textLoader, computedState: null);

    internal static async Task<RazorCodeDocument> GenerateCodeDocumentAsync(
        IDocumentSnapshot document,
        RazorProjectEngine projectEngine,
        bool forceRuntimeCodeGeneration,
        CancellationToken cancellationToken)
    {
        var importItems = await GetImportItemsAsync(document, projectEngine, cancellationToken).ConfigureAwait(false);

        return await GenerateCodeDocumentAsync(
            document, projectEngine, importItems, forceRuntimeCodeGeneration, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<RazorCodeDocument> GenerateCodeDocumentAsync(
        IDocumentSnapshot document,
        RazorProjectEngine projectEngine,
        ImmutableArray<ImportItem> imports,
        bool forceRuntimeCodeGeneration,
        CancellationToken cancellationToken)
    {
        var importSources = GetImportSources(imports, projectEngine);
        var tagHelpers = await document.Project.GetTagHelpersAsync(cancellationToken).ConfigureAwait(false);
        var source = await GetSourceAsync(document, projectEngine, cancellationToken).ConfigureAwait(false);

        return forceRuntimeCodeGeneration
            ? projectEngine.Process(source, document.FileKind, importSources, tagHelpers)
            : projectEngine.ProcessDesignTime(source, document.FileKind, importSources, tagHelpers);
    }

    private static async Task<ImmutableArray<ImportItem>> GetImportItemsAsync(
        IDocumentSnapshot document,
        RazorProjectEngine projectEngine,
        CancellationToken cancellationToken)
    {
        var projectItem = projectEngine.FileSystem.GetItem(document.FilePath, document.FileKind);

        using var importProjectItems = new PooledArrayBuilder<RazorProjectItem>();

        foreach (var feature in projectEngine.ProjectFeatures.OfType<IImportProjectFeature>())
        {
            if (feature.GetImports(projectItem) is { } featureImports)
            {
                importProjectItems.AddRange(featureImports);
            }
        }

        if (importProjectItems.Count == 0)
        {
            return [];
        }

        var project = document.Project;

        using var importItems = new PooledArrayBuilder<ImportItem>(capacity: importProjectItems.Count);

        foreach (var importProjectItem in importProjectItems)
        {
            if (importProjectItem is NotFoundProjectItem)
            {
                continue;
            }

            if (importProjectItem.PhysicalPath is null)
            {
                // This is a default import.
                using var stream = importProjectItem.Read();
                var text = SourceText.From(stream);
                var defaultImport = ImportItem.CreateDefault(text);

                importItems.Add(defaultImport);
            }
            else if (project.TryGetDocument(importProjectItem.PhysicalPath, out var importDocument))
            {
                var text = await importDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var versionStamp = await importDocument.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
                var importItem = new ImportItem(importDocument.FilePath, importDocument.FileKind, text, versionStamp);

                importItems.Add(importItem);
            }
        }

        return importItems.DrainToImmutable();
    }

    private static ImmutableArray<RazorSourceDocument> GetImportSources(ImmutableArray<ImportItem> importItems, RazorProjectEngine projectEngine)
    {
        using var importSources = new PooledArrayBuilder<RazorSourceDocument>(importItems.Length);

        foreach (var importItem in importItems)
        {
            var importProjectItem = importItem is { FilePath: string filePath, FileKind: var fileKind }
                ? projectEngine.FileSystem.GetItem(filePath, fileKind)
                : null;

            var properties = RazorSourceDocumentProperties.Create(importItem.FilePath, importProjectItem?.RelativePhysicalPath);
            var importSource = RazorSourceDocument.Create(importItem.Text, properties);

            importSources.Add(importSource);
        }

        return importSources.DrainToImmutable();
    }

    private static async Task<RazorSourceDocument> GetSourceAsync(IDocumentSnapshot document, RazorProjectEngine projectEngine, CancellationToken cancellationToken)
    {
        var projectItem = document is { FilePath: string filePath, FileKind: var fileKind }
            ? projectEngine.FileSystem.GetItem(filePath, fileKind)
            : null;

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var properties = RazorSourceDocumentProperties.Create(document.FilePath, projectItem?.RelativePhysicalPath);
        return RazorSourceDocument.Create(text, properties);
    }
}
