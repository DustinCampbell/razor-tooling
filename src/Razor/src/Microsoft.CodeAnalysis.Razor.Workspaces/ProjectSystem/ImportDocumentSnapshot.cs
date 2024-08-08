// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal class ImportDocumentSnapshot(IProjectSnapshot project, RazorProjectItem item) : IDocumentSnapshot
{
    // The default import file does not have a kind or paths.
    public string? FileKind => null;
    public string? FilePath => null;
    public string? TargetPath => null;

    public bool SupportsOutput => false;
    public IProjectSnapshot Project => _project;

    private readonly IProjectSnapshot _project = project;
    private readonly RazorProjectItem _importItem = item;
    private SourceText? _sourceText;

    public ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
    {
        return _sourceText is SourceText sourceText
            ? new(sourceText)
            : new(GetTextCore(cancellationToken));

        SourceText GetTextCore(CancellationToken cancellationToken)
        {
            using var stream = _importItem.Read();
            cancellationToken.ThrowIfCancellationRequested();

            var sourceText = SourceText.From(stream);
            cancellationToken.ThrowIfCancellationRequested();

            // Interlock to ensure that we only ever return one instance of SourceText.
            // In race scenarios, when more than one SourceText is produced, we want to
            // return whichever SourceText is cached.
            return InterlockedOperations.Initialize(ref _sourceText, sourceText);
        }
    }

    public ValueTask<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken)
        => new(VersionStamp.Default);

    public ValueTask<RazorCodeDocument> GetGeneratedOutputAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<RazorCodeDocument> GetGeneratedOutputAsync()
        => throw new NotSupportedException();

    public async Task<SourceText> GetTextAsync()
    {
        using (var stream = _importItem.Read())
        using (var reader = new StreamReader(stream))
        {
            var content = await reader.ReadToEndAsync().ConfigureAwait(false);
            _sourceText = SourceText.From(content);
        }

        return _sourceText;
    }

    public Task<VersionStamp> GetTextVersionAsync()
        => Task.FromResult(VersionStamp.Default);

    public bool TryGetText([NotNullWhen(true)] out SourceText? result)
    {
        if (_sourceText is { } sourceText)
        {
            result = sourceText;
            return true;
        }

        result = null;
        return false;
    }

    public bool TryGetTextVersion(out VersionStamp result)
    {
        result = VersionStamp.Default;
        return true;
    }

    public bool TryGetGeneratedOutput([NotNullWhen(true)] out RazorCodeDocument? result)
        => throw new NotSupportedException();

    public IDocumentSnapshot WithText(SourceText text)
        => throw new NotSupportedException();
}
