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

internal sealed class ImportDocumentSnapshot(IProjectSnapshot project, RazorProjectItem importItem) : IDocumentSnapshot
{
    public IProjectSnapshot Project { get; } = project;

    private readonly RazorProjectItem _importItem = importItem;
    private SourceText? _sourceText;

    // The default import file does not have a kind or paths.
    public string? FileKind => null;
    public string? FilePath => null;
    public string? TargetPath => null;

    public int Version => 1;

    public ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
    {
        return TryGetText(out var text)
            ? new(text)
            : GetTextCoreAsync(cancellationToken);

        ValueTask<SourceText> GetTextCoreAsync(CancellationToken cancellationToken)
        {
            using var stream = _importItem.Read();
            using var reader = new StreamReader(stream);

            var sourceText = SourceText.From(stream);
            cancellationToken.ThrowIfCancellationRequested();

            var result = InterlockedOperations.Initialize(ref _sourceText, sourceText);
            return new(result);
        }
    }

    public ValueTask<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken)
        => new(VersionStamp.Default);

    public Task<RazorCodeDocument> GetGeneratedOutputAsync(bool forceDesignTimeGeneratedOutput)
        => throw new NotSupportedException();

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

    public Task<SyntaxTree> GetCSharpSyntaxTreeAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();
}
