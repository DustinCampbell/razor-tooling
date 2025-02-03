// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Test.Common.ProjectSystem;

internal sealed class TestDocumentSnapshot : IDocumentSnapshot
{
    public DocumentSnapshot RealSnapshot { get; }

    private readonly RazorCodeDocument? _codeDocument;

    private TestDocumentSnapshot(DocumentSnapshot document, RazorCodeDocument? codeDocument = null)
    {
        RealSnapshot = document;
        _codeDocument = codeDocument;
    }

    public static TestDocumentSnapshot Create(string filePath)
        => Create(filePath, text: string.Empty, ProjectWorkspaceState.Default);

    public static TestDocumentSnapshot Create(string filePath, string text)
        => Create(filePath, text, ProjectWorkspaceState.Default);

    public static TestDocumentSnapshot Create(string filePath, string text, ProjectWorkspaceState projectWorkspaceState)
    {
        var document = CreateRealDocument(filePath, SourceText.From(text), projectWorkspaceState);

        return new TestDocumentSnapshot(document);
    }

    public static TestDocumentSnapshot Create(string filePath, RazorCodeDocument codeDocument)
        => Create(filePath, codeDocument, ProjectWorkspaceState.Create([.. codeDocument.GetTagHelpers() ?? []]));

    public static TestDocumentSnapshot Create(string filePath, RazorCodeDocument codeDocument, ProjectWorkspaceState projectWorkspaceState)
    {
        var document = CreateRealDocument(filePath, codeDocument.Source.Text, projectWorkspaceState);

        return new TestDocumentSnapshot(document, codeDocument);
    }

    private static DocumentSnapshot CreateRealDocument(string filePath, SourceText text, ProjectWorkspaceState projectWorkspaceState)
    {
        var hostProject = TestHostProject.Create(filePath + ".csproj");
        var hostDocument = TestHostDocument.Create(hostProject, filePath);

        var projectState = ProjectState
            .Create(hostProject, RazorCompilerOptions.None, ProjectEngineFactories.DefaultProvider)
            .WithProjectWorkspaceState(projectWorkspaceState)
            .AddDocument(hostDocument, text);

        var project = new ProjectSnapshot(projectState);

        return project.GetRequiredDocument(filePath);
    }

    public HostDocument HostDocument => RealSnapshot.HostDocument;

    public string FileKind => RealSnapshot.FileKind;
    public string FilePath => RealSnapshot.FilePath;
    public string TargetPath => RealSnapshot.TargetPath;
    public IProjectSnapshot Project => RealSnapshot.Project;
    public int Version => RealSnapshot.Version;

    public ValueTask<RazorCodeDocument> GetGeneratedOutputAsync(CancellationToken cancellationToken)
    {
        return _codeDocument is null
            ? RealSnapshot.GetGeneratedOutputAsync(cancellationToken)
            : new(_codeDocument);
    }

    public ValueTask<RazorSourceDocument> GetSourceAsync(CancellationToken cancellationToken)
    {
        return _codeDocument is null
            ? RealSnapshot.GetSourceAsync(cancellationToken)
            : new(_codeDocument.Source);
    }

    public ValueTask<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken)
        => RealSnapshot.GetTextVersionAsync(cancellationToken);

    public ValueTask<SyntaxTree> GetCSharpSyntaxTreeAsync(CancellationToken cancellationToken)
    {
        return _codeDocument is null
            ? RealSnapshot.GetCSharpSyntaxTreeAsync(cancellationToken)
            : new(_codeDocument.GetOrParseCSharpSyntaxTree(cancellationToken));
    }

    public bool TryGetGeneratedOutput([NotNullWhen(true)] out RazorCodeDocument? result)
    {
        if (_codeDocument is { } codeDocument)
        {
            result = codeDocument;
            return true;
        }

        return RealSnapshot.TryGetGeneratedOutput(out result);
    }

    public bool TryGetSource([NotNullWhen(true)] out RazorSourceDocument? result)
    {
        if (_codeDocument is { } codeDocument)
        {
            result = codeDocument.Source;
            return true;
        }

        return RealSnapshot.TryGetSource(out result);
    }

    public bool TryGetTextVersion(out VersionStamp result)
        => RealSnapshot.TryGetTextVersion(out result);

    public IDocumentSnapshot WithText(SourceText text)
        => RealSnapshot.WithText(text);
}
