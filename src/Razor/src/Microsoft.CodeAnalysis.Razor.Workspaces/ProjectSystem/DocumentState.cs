// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
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

    private DocumentState(HostDocument hostDocument, RazorSourceDocumentProperties properties, ISourceAndVersionSource sourceAndVersionSource)
    {
        HostDocument = hostDocument;
        Version = 1;
        _properties = properties;
        _sourceAndVersionSource = sourceAndVersionSource;
        _generatedOutputSource = new();
    }

    private DocumentState(DocumentState oldState, ISourceAndVersionSource sourceAndVersionSource)
    {
        HostDocument = oldState.HostDocument;
        Version = oldState.Version + 1;
        _properties = oldState._properties;
        _sourceAndVersionSource = sourceAndVersionSource;
        _generatedOutputSource = new();
    }

    public static DocumentState Create(HostDocument hostDocument, RazorProjectItem projectItem, SourceText text)
    {
        var properties = RazorSourceDocumentProperties.Create(projectItem.FilePath, projectItem.RelativePhysicalPath);
        return new(hostDocument, properties, CreateSourceAndVersionSource(text, properties));
    }

    public static DocumentState Create(HostDocument hostDocument, RazorProjectItem projectItem, TextLoader textLoader)
    {
        var properties = RazorSourceDocumentProperties.Create(projectItem.FilePath, projectItem.RelativePhysicalPath);
        return new(hostDocument, properties, CreateSourceAndVersionSource(textLoader, properties));
    }

    private static ConstantSourceAndVersionSource CreateSourceAndVersionSource(
        SourceText text,
        RazorSourceDocumentProperties properties,
        VersionStamp? version = null)
        => new(RazorSourceDocument.Create(text, properties), version ?? VersionStamp.Create());

    private static LoadableSourceAndVersionSource CreateSourceAndVersionSource(
        TextLoader textLoader,
        RazorSourceDocumentProperties properties)
        => new(textLoader, properties);

    public static DocumentState Create(HostDocument hostDocument, SourceText text)
    {
        var properties = RazorSourceDocumentProperties.Create(hostDocument.FilePath, hostDocument.TargetPath);
        return new(hostDocument, properties, CreateSourceAndVersionSource(text, properties));
    }

    public static DocumentState Create(HostDocument hostDocument, TextLoader textLoader)
    {
        var properties = RazorSourceDocumentProperties.Create(hostDocument.FilePath, hostDocument.TargetPath);
        return new(hostDocument, properties, CreateSourceAndVersionSource(textLoader, properties));
    }

    public bool TryGetGeneratedOutput([NotNullWhen(true)] out RazorCodeDocument? result)
        => _generatedOutputSource.TryGetValue(out result);

    public ValueTask<RazorCodeDocument> GetGeneratedOutputAsync(DocumentSnapshot document, CancellationToken cancellationToken)
        => _generatedOutputSource.GetValueAsync(document, cancellationToken);

    public bool TryGetSourceAndVersion([NotNullWhen(true)] out SourceAndVersion? result)
        => _sourceAndVersionSource.TryGetValue(out result);

    public ValueTask<SourceAndVersion> GetSourceAndVersionAsync(CancellationToken cancellationToken)
        => _sourceAndVersionSource.GetValueAsync(cancellationToken);

    public bool TryGetText([NotNullWhen(true)] out SourceText? result)
    {
        if (TryGetSourceAndVersion(out var sourceAndVersion))
        {
            result = sourceAndVersion.Source.Text;
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
            var sourceAsVersion = await GetSourceAndVersionAsync(cancellationToken).ConfigureAwait(false);

            return sourceAsVersion.Source.Text;
        }
    }

    public bool TryGetTextVersion(out VersionStamp result)
    {
        if (TryGetSourceAndVersion(out var textAndVersion))
        {
            result = textAndVersion.Version;
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

    public DocumentState WithImportsChange()
        => new(this, _sourceAndVersionSource);

    public DocumentState WithProjectWorkspaceStateChange()
        => new(this, _sourceAndVersionSource);

    public DocumentState WithText(SourceText text, VersionStamp textVersion)
        => new(this, CreateSourceAndVersionSource(text, _properties, textVersion));

    public DocumentState WithTextLoader(TextLoader textLoader)
        => ReferenceEquals(textLoader, _sourceAndVersionSource.TextLoader)
            ? this
            : new(this, CreateSourceAndVersionSource(textLoader, _properties));
}
