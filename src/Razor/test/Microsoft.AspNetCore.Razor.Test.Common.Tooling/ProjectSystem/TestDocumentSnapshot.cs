// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Test.Common.ProjectSystem;

internal class TestDocumentSnapshot(ProjectSnapshot projectSnapshot, DocumentState documentState)
    : DocumentSnapshot(projectSnapshot, documentState)
{
    private RazorCodeDocument? _codeDocument;

    public static TestDocumentSnapshot Create(string filePath)
        => Create(filePath, string.Empty);

    public static TestDocumentSnapshot Create(string filePath, string text, int documentVersion = 0)
        => Create(filePath, text, VersionStamp.Default, documentVersion: documentVersion);

    public static TestDocumentSnapshot Create(string filePath, string text, VersionStamp textVersion, ProjectWorkspaceState? projectWorkspaceState = null, int documentVersion = 0)
        => Create(filePath, text, textVersion, TestProjectSnapshot.Create(filePath + ".csproj", projectWorkspaceState), documentVersion);

    public static TestDocumentSnapshot Create(string filePath, string text, VersionStamp textVersion, TestProjectSnapshot projectSnapshot, int documentVersion)
    {
        var targetPath = Path.GetDirectoryName(projectSnapshot.FilePath) is string projectDirectory && filePath.StartsWith(projectDirectory)
            ? filePath[projectDirectory.Length..]
            : filePath;

        var hostDocument = new HostDocument(filePath, targetPath);
        var textAndVersion = TextAndVersion.Create(SourceText.From(text), textVersion);
        var documentState = DocumentState.Create(hostDocument, documentVersion, textAndVersion);

        return new TestDocumentSnapshot(projectSnapshot, documentState);
   }

    internal static TestDocumentSnapshot Create(ProjectSnapshot projectSnapshot, string filePath, string text = "")
    {
        var targetPath = FilePathNormalizer.Normalize(filePath);
        var projectDirectory = FilePathNormalizer.GetNormalizedDirectoryName(projectSnapshot.FilePath);
        if (targetPath.StartsWith(projectDirectory))
        {
            targetPath = targetPath[projectDirectory.Length..];
        }

        var hostDocument = new HostDocument(filePath, targetPath);
        var textAndVersion = TextAndVersion.Create(SourceText.From(text), VersionStamp.Default);
        var documentState = DocumentState.Create(hostDocument, version: 1, textAndVersion);

        return new TestDocumentSnapshot(projectSnapshot, documentState);
    }

    public override Task<RazorCodeDocument> GetGeneratedOutputAsync(bool _)
    {
        if (_codeDocument is null)
        {
            throw new ArgumentNullException(nameof(_codeDocument));
        }

        return Task.FromResult(_codeDocument);
    }

    public override bool TryGetGeneratedOutput(out RazorCodeDocument result)
    {
        if (_codeDocument is null)
        {
            throw new InvalidOperationException($"You must call {nameof(With)} to set the code document for this document snapshot.");
        }

        result = _codeDocument;
        return true;
    }

    public TestDocumentSnapshot With(RazorCodeDocument codeDocument)
    {
        if (codeDocument is null)
        {
            throw new ArgumentNullException(nameof(codeDocument));
        }

        _codeDocument = codeDocument;
        return this;
    }
}
