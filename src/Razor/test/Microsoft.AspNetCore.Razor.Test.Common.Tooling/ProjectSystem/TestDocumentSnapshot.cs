﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Test.Common.ProjectSystem;

internal sealed class TestDocumentSnapshot : IRazorDocument
{
    public RazorDocument RealDocument { get; }

    private readonly RazorCodeDocument? _codeDocument;

    private TestDocumentSnapshot(TestProjectSnapshot project, DocumentState state, RazorCodeDocument? codeDocument = null)
    {
        RealDocument = new RazorDocument(project.RealSnapshot, state);
        _codeDocument = codeDocument;
    }

    public static TestDocumentSnapshot Create(string filePath)
        => Create(filePath, text: string.Empty, ProjectWorkspaceState.Default);

    public static TestDocumentSnapshot Create(string filePath, string text)
        => Create(filePath, text, ProjectWorkspaceState.Default);

    public static TestDocumentSnapshot Create(string filePath, string text, ProjectWorkspaceState projectWorkspaceState)
    {
        var project = TestProjectSnapshot.Create(filePath + ".csproj", projectWorkspaceState);
        var hostDocument = TestHostDocument.Create(project.HostProject, filePath);

        var sourceText = SourceText.From(text);

        var documentState = DocumentState.Create(hostDocument, sourceText);

        return new TestDocumentSnapshot(project, documentState);
    }

    public static TestDocumentSnapshot Create(string filePath, RazorCodeDocument codeDocument)
        => Create(filePath, codeDocument, ProjectWorkspaceState.Create([.. codeDocument.GetTagHelpers() ?? []]));

    public static TestDocumentSnapshot Create(string filePath, RazorCodeDocument codeDocument, ProjectWorkspaceState projectWorkspaceState)
    {
        var project = TestProjectSnapshot.Create(filePath + ".csproj", projectWorkspaceState);
        var hostDocument = TestHostDocument.Create(project.HostProject, filePath);

        var sourceText = codeDocument.Source.Text;

        var documentState = DocumentState.Create(hostDocument, sourceText);

        return new TestDocumentSnapshot(project, documentState, codeDocument);
    }

    public HostDocument HostDocument => RealDocument.HostDocument;

    public string FileKind => RealDocument.FileKind;
    public string FilePath => RealDocument.FilePath;
    public string TargetPath => RealDocument.TargetPath;
    public IRazorProject Project => RealDocument.Project;
    public int Version => RealDocument.Version;

    public ValueTask<RazorCodeDocument> GetGeneratedOutputAsync(CancellationToken cancellationToken)
    {
        return _codeDocument is null
            ? RealDocument.GetGeneratedOutputAsync(cancellationToken)
            : new(_codeDocument);
    }

    public ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
    {
        return _codeDocument is null
            ? RealDocument.GetTextAsync(cancellationToken)
            : new(_codeDocument.Source.Text);
    }

    public ValueTask<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken)
        => RealDocument.GetTextVersionAsync(cancellationToken);

    public ValueTask<SyntaxTree> GetCSharpSyntaxTreeAsync(CancellationToken cancellationToken)
    {
        return _codeDocument is null
            ? RealDocument.GetCSharpSyntaxTreeAsync(cancellationToken)
            : new(_codeDocument.GetOrParseCSharpSyntaxTree(cancellationToken));
    }

    public bool TryGetGeneratedOutput([NotNullWhen(true)] out RazorCodeDocument? result)
    {
        if (_codeDocument is { } codeDocument)
        {
            result = codeDocument;
            return true;
        }

        return RealDocument.TryGetGeneratedOutput(out result);
    }

    public bool TryGetText([NotNullWhen(true)] out SourceText? result)
    {
        if (_codeDocument is { } codeDocument)
        {
            result = codeDocument.Source.Text;
            return true;
        }

        return RealDocument.TryGetText(out result);
    }

    public bool TryGetTextVersion(out VersionStamp result)
        => RealDocument.TryGetTextVersion(out result);

    public IRazorDocument WithText(SourceText text)
        => RealDocument.WithText(text);
}
