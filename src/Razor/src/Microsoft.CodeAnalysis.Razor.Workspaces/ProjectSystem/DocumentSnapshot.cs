// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal class DocumentSnapshot(ProjectSnapshot project, DocumentState state) : IDocumentSnapshot
{
    private readonly ProjectSnapshot _project = project;
    private readonly DocumentState _state = state;

    public string FileKind => HostDocument.FileKind;
    public string FilePath => HostDocument.FilePath;
    public string TargetPath => HostDocument.TargetPath;

    public IProjectSnapshot Project => _project;

    public int Version => _state.Version;

    public HostDocument HostDocument => _state.HostDocument;

    public ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
        => _state.GetTextAsync(cancellationToken);

    public ValueTask<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken)
        => _state.GetTextVersionAsync(cancellationToken);

    public bool TryGetText([NotNullWhen(true)] out SourceText? result)
        => _state.TryGetText(out result);

    public bool TryGetTextVersion(out VersionStamp result)
        => _state.TryGetTextVersion(out result);

    public virtual bool TryGetGeneratedOutput([NotNullWhen(true)] out RazorCodeDocument? result)
    {
        if (_state.IsGeneratedOutputResultAvailable)
        {
            result = _state.GetGeneratedOutputAndVersionAsync(_project, this).VerifyCompleted().output;
            return true;
        }

        result = null;
        return false;
    }

    public IDocumentSnapshot WithText(SourceText text)
    {
        return new DocumentSnapshot(_project, _state.WithText(text, VersionStamp.Create()));
    }

    public async Task<SyntaxTree> GetCSharpSyntaxTreeAsync(CancellationToken cancellationToken)
    {
        var codeDocument = await GetGeneratedOutputAsync(forceDesignTimeGeneratedOutput: false, cancellationToken).ConfigureAwait(false);
        var csharpText = codeDocument.GetCSharpSourceText();
        return CSharpSyntaxTree.ParseText(csharpText, cancellationToken: cancellationToken);
    }

    public virtual async ValueTask<RazorCodeDocument> GetGeneratedOutputAsync(bool forceDesignTimeGeneratedOutput, CancellationToken cancellationToken)
    {
        if (forceDesignTimeGeneratedOutput)
        {
            return await GetDesignTimeGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
        }

        var (output, _) = await _state.GetGeneratedOutputAndVersionAsync(_project, this).ConfigureAwait(false);
        return output;
    }

    private async Task<RazorCodeDocument> GetDesignTimeGeneratedOutputAsync(CancellationToken cancellationToken)
    {
        var tagHelpers = await _project.GetTagHelpersAsync(cancellationToken).ConfigureAwait(false);
        var projectEngine = _project.GetProjectEngine();
        var imports = await this.GetImportsAsync(projectEngine).ConfigureAwait(false);
        return await this.GenerateCodeDocumentAsync(projectEngine, imports, tagHelpers, forceRuntimeCodeGeneration: false).ConfigureAwait(false);
    }
}
