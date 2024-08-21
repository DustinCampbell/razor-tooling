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

internal sealed class ImportDocumentSnapshot(IProjectSnapshot project, RazorProjectItem projectItem) : IDocumentSnapshot
{
    private static readonly Task<VersionStamp> s_version = Task.FromResult(VersionStamp.Default);

    public IProjectSnapshot Project { get; } = project;
    private readonly RazorProjectItem _projectItem = projectItem;

    private SourceText? _text;

    // The default import file does not have a kind or paths.
    public string? FileKind => null;
    public string? FilePath => null;
    public string? TargetPath => null;

    public Task<SourceText> GetTextAsync()
    {
        return _text is SourceText text
            ? Task.FromResult(text)
            : GetTextCoreAsync();

        Task<SourceText> GetTextCoreAsync()
        {
            using var stream = _projectItem.Read();
            using var reader = new StreamReader(stream);

            var length = (int)stream.Length;
            var text = SourceText.From(reader, length);

            var result = InterlockedOperations.Initialize(ref _text, text);

            return Task.FromResult(result);
        }
    }

    public Task<VersionStamp> GetTextVersionAsync()
        => s_version;

    public Task<RazorCodeDocument> GetGeneratedOutputAsync()
        => throw new NotSupportedException();

    public bool TryGetText([NotNullWhen(true)] out SourceText? result)
    {
        if (_text is SourceText text)
        {
            result = text;
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
