// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal sealed partial class DocumentState
{
    public HostDocument HostDocument { get; }
    public int Version { get; }

    private TextAndVersion? _textAndVersion;
    private readonly TextLoader _textLoader;

    // SemaphoreSlim instance does not need to be disposed if SemaphoreSlim.AvailableWaitHandle is not accessed.
    private readonly SemaphoreSlim _gate = new(initialCount: 1);
    private WeakReference<OutputAndVersion>? _weakComputedState;

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
        OutputAndVersion? computedState)
    {
        HostDocument = other.HostDocument;
        Version = other.Version + 1;
        _textAndVersion = textAndVersion;
        _textLoader = textLoader ?? EmptyTextLoader.Instance;

        if (computedState is not null)
        {
            _weakComputedState = new(computedState);
        }
    }

    public static DocumentState Create(HostDocument hostDocument, TextAndVersion textAndVersion)
        => new(hostDocument, textAndVersion, textLoader: null);

    public static DocumentState Create(HostDocument hostDocument, TextLoader textLoader)
        => new(hostDocument, textAndVersion: null, textLoader);

    public static DocumentState Create(HostDocument hostDocument)
        => new(hostDocument, textAndVersion: null, textLoader: null);

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

    public async Task<OutputAndVersion> GetGeneratedOutputAndVersionAsync(DocumentSnapshot document, RazorProjectEngine projectEngine, CancellationToken cancellationToken)
    {
        var project = document.Project;
        var importItems = await document.GetImportItemsAsync(projectEngine, cancellationToken).ConfigureAwait(false);
        var version = await GetLatestVersionAsync(document, importItems, cancellationToken).ConfigureAwait(false);

        using var _ = await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false);

        var weakOutputAndVersion = _weakComputedState;

        // Do we already have cached output with the same version? If so, there's no reason to re-generate it.
        if (weakOutputAndVersion is not null &&
            weakOutputAndVersion.TryGetTarget(out var result) &&
            result.Version == version)
        {
            return result;
        }

        var forceRuntimeCodeGeneration = project.CompilerOptions.HasFlag(RazorCompilerOptions.ForceRuntimeCodeGeneration);
        var codeDocument = await document.GenerateCodeDocumentAsync(projectEngine, importItems, forceRuntimeCodeGeneration, cancellationToken).ConfigureAwait(false);

        result = new OutputAndVersion(codeDocument, version);

        if (weakOutputAndVersion is not null)
        {
            weakOutputAndVersion.SetTarget(result);
        }
        else
        {
            _weakComputedState = new(result);
        }

        return result;
    }

    private static async ValueTask<VersionStamp> GetLatestVersionAsync(
        DocumentSnapshot document,
        ImmutableArray<ImportItem> importItems,
        CancellationToken cancellationToken)
    {
        // We only need to produce the generated code if any of our inputs is newer than the
        // previously cached output.
        //
        // First find the versions that are the inputs:
        // - The project + computed state
        // - The imports
        // - This document
        //
        // All of these things are cached, so no work is wasted if we do need to generate the code.

        var version = await document.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);

        version = version.GetNewerVersion(document.Project.GetLatestVersion());

        foreach (var import in importItems)
        {
            version = version.GetNewerVersion(import.Version);
        }

        return version;
    }

    public bool TryGetText([NotNullWhen(true)] out SourceText? result)
    {
        if (_textAndVersion is { Text: var text })
        {
            result = text;
            return true;
        }

        result = null;
        return false;
    }

    public bool TryGetTextVersion(out VersionStamp result)
    {
        if (_textAndVersion is { Version: var version })
        {
            result = version;
            return true;
        }

        result = default;
        return false;
    }

    public bool TryGetGeneratedOutputAndVersion([NotNullWhen(true)] out OutputAndVersion? result)
    {
        result = GetComputedStateOrNull();
        return result is not null;
    }

    private OutputAndVersion? GetComputedStateOrNull()
    {
        using (_gate.DisposableWait())
        {
            if (_weakComputedState is { } weakComputedState &&
                weakComputedState.TryGetTarget(out var result))
            {
                return result;
            }
        }

        return null;
    }

    public DocumentState WithConfigurationChange()
        => new(this, _textAndVersion, _textLoader, computedState: null);

    public DocumentState WithImportsChange()
        // Optimistically cache the computed state
        => new(this, _textAndVersion, _textLoader, computedState: GetComputedStateOrNull());

    public DocumentState WithProjectWorkspaceStateChange()
        // Optimistically cache the computed state
        => new(this, _textAndVersion, _textLoader, computedState: GetComputedStateOrNull());

    public DocumentState WithText(SourceText text, VersionStamp textVersion)
        => new(this, TextAndVersion.Create(text, textVersion), textLoader: null, computedState: null);

    public DocumentState WithTextLoader(TextLoader textLoader)
        => new(this, textAndVersion: null, textLoader, computedState: null);
}
