// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal class DocumentSnapshot(ProjectSnapshot project, DocumentState state) : IDocumentSnapshot
{
    public string FileKind => State.HostDocument.FileKind;
    public string FilePath => State.HostDocument.FilePath;
    public string TargetPath => State.HostDocument.TargetPath;
    public IProjectSnapshot Project => ProjectInternal;
    public bool SupportsOutput => true;

    public ProjectSnapshot ProjectInternal { get; } = project;
    public DocumentState State { get; } = state;

    public ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
        => State.GetTextAsync(cancellationToken);

    public ValueTask<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken)
        => State.GetTextVersionAsync(cancellationToken);

    public async ValueTask<RazorCodeDocument> GetGeneratedOutputAsync(CancellationToken cancellationToken)
    {
        var (output, _) = await State.GetGeneratedOutputAndVersionAsync(ProjectInternal, this).ConfigureAwait(false);
        return output;
    }

    public Task<SourceText> GetTextAsync()
        => State.GetTextAsync();

    public Task<VersionStamp> GetTextVersionAsync()
        => State.GetTextVersionAsync();

    public virtual async Task<RazorCodeDocument> GetGeneratedOutputAsync()
    {
        var (output, _) = await State.GetGeneratedOutputAndVersionAsync(ProjectInternal, this).ConfigureAwait(false);
        return output;
    }

    public bool TryGetText([NotNullWhen(true)] out SourceText? result)
        => State.TryGetText(out result);

    public bool TryGetTextVersion(out VersionStamp result)
        => State.TryGetTextVersion(out result);

    public virtual bool TryGetGeneratedOutput([NotNullWhen(true)] out RazorCodeDocument? result)
    {
        if (State.IsGeneratedOutputResultAvailable)
        {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            result = State.GetGeneratedOutputAndVersionAsync(ProjectInternal, this).Result.output;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
            return true;
        }

        result = null;
        return false;
    }

    public IDocumentSnapshot WithText(SourceText text)
    {
        return new DocumentSnapshot(ProjectInternal, State.WithText(text, VersionStamp.Create()));
    }
}
