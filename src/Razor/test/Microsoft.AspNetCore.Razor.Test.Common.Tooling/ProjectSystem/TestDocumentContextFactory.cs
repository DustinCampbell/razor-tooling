// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.Test.Common.ProjectSystem;

internal sealed class TestDocumentContextFactory : IDocumentContextFactory
{
    private readonly Dictionary<string, DocumentContext> _filePathToContextMap = new(FilePathNormalizingComparer.Instance);

    public TestDocumentContextFactory()
    {
    }

    public TestDocumentContextFactory(string filePath, RazorCodeDocument codeDocument)
    {
        Add(filePath, codeDocument);
    }

    public void Add(string filePath, RazorCodeDocument codeDocument)
    {
        _filePathToContextMap.Add(filePath, TestDocumentContext.Create(filePath, codeDocument));
    }

    public void Add(string filePath, string content)
    {
        _filePathToContextMap.Add(filePath, TestDocumentContext.Create(filePath, content));
    }

    public bool TryCreate(
        Uri documentUri,
        VSProjectContext? projectContext,
        [NotNullWhen(true)] out DocumentContext? context)
    {
        if (_filePathToContextMap.TryGetValue(documentUri.GetAbsoluteOrUNCPath(), out var value))
        {
            context = value;
            return true;
        }

        context = null;
        return false;
    }
}
