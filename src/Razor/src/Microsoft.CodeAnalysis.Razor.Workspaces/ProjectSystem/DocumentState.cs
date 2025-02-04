// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem.Sources;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal sealed partial class DocumentState
{
    public HostDocument HostDocument { get; }
    public int Version { get; }

    private readonly RazorSourceDocumentProperties _properties;
    private readonly ISourceAndVersionSource _sourceAndVersionSource;
    private readonly GeneratedOutputSource _generatedOutputSource;
    private readonly ILogger _logger;

    private DocumentState(
        HostDocument hostDocument,
        RazorSourceDocumentProperties properties,
        ISourceAndVersionSource sourceAndVersionSource,
        ILogger logger)
    {
        HostDocument = hostDocument;
        Version = 1;
        _properties = properties;
        _sourceAndVersionSource = sourceAndVersionSource;
        _generatedOutputSource = new(logger);
        _logger = logger;
    }

    private DocumentState(DocumentState oldState, ISourceAndVersionSource sourceAndVersionSource)
    {
        HostDocument = oldState.HostDocument;
        Version = oldState.Version + 1;
        _properties = oldState._properties;
        _sourceAndVersionSource = sourceAndVersionSource;
        _generatedOutputSource = new(oldState._logger);
        _logger = oldState._logger;
    }

    public static DocumentState Create(
        HostDocument hostDocument,
        RazorSourceDocumentProperties properties,
        SourceText text,
        ILogger? logger = null)
        => new(
            hostDocument,
            properties,
            CreateSourceAndVersionSource(text, properties),
            logger ?? EmptyLoggerFactory.Instance.GetOrCreateLogger("DocumentState"));

    public static DocumentState Create(
        HostDocument hostDocument,
        RazorSourceDocumentProperties properties,
        TextLoader textLoader,
        ILogger? logger = null)
        => new(
            hostDocument,
            properties,
            CreateSourceAndVersionSource(textLoader, properties),
            logger ?? EmptyLoggerFactory.Instance.GetOrCreateLogger("DocumentState"));

    private static ConstantSourceAndVersionSource CreateSourceAndVersionSource(
        SourceText text,
        RazorSourceDocumentProperties properties,
        VersionStamp? version = null)
        => new(RazorSourceDocument.Create(text, properties), version ?? VersionStamp.Create());

    private static LoadableSourceAndVersionSource CreateSourceAndVersionSource(
        TextLoader textLoader,
        RazorSourceDocumentProperties properties)
        => new(textLoader, properties);

    public bool TryGetGeneratedOutput([NotNullWhen(true)] out RazorCodeDocument? result)
    {
        result = _generatedOutputSource.TryGetValue(out var generatedOutput)
            ? generatedOutput.CodeDocument
            : null;

        return result is not null;
    }

    public ValueTask<RazorCodeDocument> GetGeneratedOutputAsync(DocumentSnapshot document, CancellationToken cancellationToken)
    {
        return TryGetGeneratedOutput(out var result)
            ? new(result)
            : GetGeneratedOutputCoreAsync(document, cancellationToken);

        async ValueTask<RazorCodeDocument> GetGeneratedOutputCoreAsync(DocumentSnapshot document, CancellationToken cancellationToken)
        {
            var output = await _generatedOutputSource
                .GetValueAsync(document, cancellationToken)
                .ConfigureAwait(false);

            return output.CodeDocument;
        }
    }

    public bool TryGetSourceAndVersion([NotNullWhen(true)] out SourceAndVersion? result)
        => _sourceAndVersionSource.TryGetValue(out result);

    public ValueTask<SourceAndVersion> GetSourceAndVersionAsync(CancellationToken cancellationToken)
        => _sourceAndVersionSource.GetValueAsync(cancellationToken);

    public bool TryGetSource([NotNullWhen(true)] out RazorSourceDocument? result)
    {
        result = TryGetSourceAndVersion(out var sourceAndVersion)
            ? sourceAndVersion.Source
            : null;

        return result is not null;
    }

    public ValueTask<RazorSourceDocument> GetSourceAsync(CancellationToken cancellationToken)
    {
        return TryGetSource(out var result)
            ? new(result)
            : GetSourceCoreAsync(cancellationToken);

        async ValueTask<RazorSourceDocument> GetSourceCoreAsync(CancellationToken cancellationToken)
        {
            var sourceAsVersion = await GetSourceAndVersionAsync(cancellationToken).ConfigureAwait(false);

            return sourceAsVersion.Source;
        }
    }

    public bool TryGetText([NotNullWhen(true)] out SourceText? result)
    {
        if (TryGetSource(out var source))
        {
            result = source.Text;
            return true;
        }

        result = null;
        return false;
    }

    public ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
    {
        return TryGetText(out var text)
            ? new(text)
            : GetTextCoreAsync(cancellationToken);

        async ValueTask<SourceText> GetTextCoreAsync(CancellationToken cancellationToken)
        {
            var source = await GetSourceAsync(cancellationToken).ConfigureAwait(false);

            return source.Text;
        }
    }

    public bool TryGetTextVersion(out VersionStamp result)
    {
        if (TryGetSourceAndVersion(out var sourceAndVersion))
        {
            result = sourceAndVersion.Version;
            return true;
        }

        result = default;
        return false;
    }

    public ValueTask<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken)
    {
        return TryGetTextVersion(out var version)
            ? new(version)
            : GetTextVersionCoreAsync(cancellationToken);

        async ValueTask<VersionStamp> GetTextVersionCoreAsync(CancellationToken cancellationToken)
        {
            var sourceAndVersion = await GetSourceAndVersionAsync(cancellationToken).ConfigureAwait(false);

            return sourceAndVersion.Version;
        }
    }

    public DocumentState WithConfigurationChange()
        => new(this, _sourceAndVersionSource);

    public DocumentState WithImportsChange(VersionStamp? importsVersion)
    {
        // If we've already generated output for this document and the imports version matches,
        // we don't need to do anything.
        if (importsVersion.HasValue &&
            _generatedOutputSource.TryGetValue(out var generatedOutput) &&
            generatedOutput.ImportsVersion == importsVersion.GetValueOrDefault())
        {
            Debug.WriteLine($"[DocumentState] Skipping imports change - {HostDocument.FilePath}.");
            return this;
        }

        return new(this, _sourceAndVersionSource);
    }

    public DocumentState WithProjectWorkspaceStateChange()
        => new(this, _sourceAndVersionSource);

    public DocumentState WithText(SourceText text)
    {
        // First, see if the RazorSourceDocument has already been created.
        if (TryGetSourceAndVersion(out var oldSourceAndVersion))
        {
            // If the text is the same, we don't need to do anything.
            if (text.ContentEquals(oldSourceAndVersion.Source.Text))
            {
                return this;
            }

            // If the text is different, acquire a newer version and create new DocumentState.
            var newVersion = oldSourceAndVersion.Version.GetNewerVersion();

            return new(this, CreateSourceAndVersionSource(text, _properties, newVersion));
        }

        // If the RazorSourceDocument hasn't been created yet, we can create it with the
        // SourceText that was passed in and a new version.
        return new(this, CreateSourceAndVersionSource(text, _properties, VersionStamp.Create()));
    }

    public DocumentState WithTextLoader(TextLoader textLoader)
    {
        // If the ISourceAndVersionSource is using the same text loader, we don't need to do anything.
        if (ReferenceEquals(textLoader, _sourceAndVersionSource.TextLoader))
        {
            return this;
        }

        return new(this, CreateSourceAndVersionSource(textLoader, _properties));
    }
}
