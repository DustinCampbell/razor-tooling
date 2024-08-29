// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.LanguageServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion;

public abstract class TagHelperServiceTestBase(ITestOutputHelper testOutput) : LanguageServerTestBase(testOutput)
{
    protected static ImmutableArray<TagHelperDescriptor> DefaultTagHelpers => TestTagHelperData.DefaultTagHelpers;

    protected static string GetFileName(bool isRazorFile)
        => TestRazorCodeDocumentFactory.GetFileName(isRazorFile);

    internal static RazorCodeDocument CreateCodeDocument(string text, bool isRazorFile, params ImmutableArray<TagHelperDescriptor> tagHelpers)
        => TestRazorCodeDocumentFactory.Create(text, isRazorFile, tagHelpers);

    internal static RazorCodeDocument CreateCodeDocument(string text, string filePath, params ImmutableArray<TagHelperDescriptor> tagHelpers)
    {
        return TestRazorCodeDocumentFactory.Create(text, filePath, tagHelpers);
    }
}
