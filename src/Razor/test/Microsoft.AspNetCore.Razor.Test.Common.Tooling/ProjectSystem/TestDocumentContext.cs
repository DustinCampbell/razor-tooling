// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.Test.Common.ProjectSystem;

internal static class TestDocumentContext
{
    public static DocumentContext Create(Uri uri)
        => Create(uri, string.Empty);

    public static DocumentContext Create(Uri uri, string text)
    {
        var filePath = uri.GetAbsoluteOrUNCPath();
        var hostProject = TestHostProject.Create(filePath + ".csproj");
        var hostDocument = TestHostDocument.Create(hostProject, filePath);

        var document = RazorProject
            .Create(hostProject, RazorCompilerOptions.None, ProjectEngineFactories.DefaultProvider)
            .AddDocument(hostDocument, SourceText.From(text))
            .GetRequiredDocument(filePath);

        return new DocumentContext(uri, document, projectContext: null);
    }

    public static DocumentContext Create(string filePath, string text)
        => Create(CreateUri(filePath), text);

    public static DocumentContext Create(string filePath, RazorCodeDocument codeDocument)
    {
        var uri = CreateUri(filePath);
        var document = TestMocks.CreateDocument(filePath, codeDocument);

        return new DocumentContext(uri, document, projectContext: null);
    }

    public static DocumentContext Create(string filePath)
    {
        var properties = RazorSourceDocumentProperties.Create(filePath, relativePath: filePath);
        var sourceDocument = RazorSourceDocument.Create(content: string.Empty, properties);
        var codeDocument = RazorCodeDocument.Create(sourceDocument);

        return Create(filePath, codeDocument);
    }

    private static Uri CreateUri(string filePath)
    {
        try
        {
            return new Uri(filePath);
        }
        catch
        {
            return VsLspFactory.CreateFilePathUri(filePath);
        }
    }
}
