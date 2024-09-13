// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
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

    public bool IsGeneratedOutputResultAvailable => ComputedState.IsResultAvailable;

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

    public ValueTask<TextAndVersion> GetTextAndVersionAsync(CancellationToken cancellationToken)
    {
        return _textAndVersion is TextAndVersion textAndVersion
            ? new(textAndVersion)
            : GetTextAndVersionCoreAsync(cancellationToken);

        async ValueTask<TextAndVersion> GetTextAndVersionCoreAsync(CancellationToken cancellationToken)
        {
            var textAndVersion = await _loader().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var result = InterlockedOperations.Initialize(ref _textAndVersion, textAndVersion);

            return result;
        }
    }

    public ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
    {
        return TryGetText(out var text)
            ? new(text)
            : GetTextCoreAsync(cancellationToken);

        async ValueTask<SourceText> GetTextCoreAsync(CancellationToken cancellationToken)
        {
            var textAndVersion = await GetTextAndVersionAsync(cancellationToken).ConfigureAwait(false);
            return textAndVersion.Text;
        }
    }

    public ValueTask<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken)
    {
        return TryGetTextVersion(out var version)
            ? new(version)
            : GetTextVersionCoreAsync(cancellationToken);

        async ValueTask<VersionStamp> GetTextVersionCoreAsync(CancellationToken cancellationToken)
        {
            var textAndVersion = await GetTextAndVersionAsync(cancellationToken).ConfigureAwait(false);
            return textAndVersion.Version;
        }
    }

    public bool TryGetTextAndVersion([NotNullWhen(true)] out TextAndVersion? result)
    {
        if (_textAndVersion is TextAndVersion textAndVersion)
        {
            result = textAndVersion;
            return true;
        }

        result = null;
        return false;
    }

    public bool TryGetText([NotNullWhen(true)] out SourceText? result)
    {
        if (TryGetTextAndVersion(out var textAndVersion))
        {
            result = textAndVersion.Text;
            return true;
        }

        result = null;
        return false;
    }

    public bool TryGetTextVersion(out VersionStamp result)
    {
        if (TryGetTextAndVersion(out var textAndVersion))
        {
            result = textAndVersion.Version;
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
}
