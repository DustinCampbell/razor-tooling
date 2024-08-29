// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.LanguageServer;
using Microsoft.CodeAnalysis;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion;

public abstract class TagHelperServiceTestBase(ITestOutputHelper testOutput) : LanguageServerTestBase(testOutput)
{
    protected const string CSHtmlFile = "test.cshtml";
    protected const string RazorFile = "test.razor";

    protected static ImmutableArray<TagHelperDescriptor> DefaultTagHelpers => TestTagHelperData.DefaultTagHelpers;

    protected static string GetFileName(bool isRazorFile)
        => isRazorFile ? RazorFile : CSHtmlFile;

    internal static RazorCodeDocument CreateCodeDocument(string text, bool isRazorFile, params ImmutableArray<TagHelperDescriptor> tagHelpers)
        => CreateCodeDocument(text, GetFileName(isRazorFile), tagHelpers);

    internal static RazorCodeDocument CreateCodeDocument(string text, string filePath, params ImmutableArray<TagHelperDescriptor> tagHelpers)
    {
        tagHelpers = tagHelpers.NullToEmpty();

        var sourceDocument = TestRazorSourceDocument.Create(text, filePath: filePath, relativePath: filePath);
        var projectEngine = RazorProjectEngine.Create(builder => { });
        var fileKind = filePath.EndsWith(".razor", StringComparison.Ordinal) ? FileKinds.Component : FileKinds.Legacy;

        return projectEngine.ProcessDesignTime(sourceDocument, fileKind, importSources: default, tagHelpers);
    }
}
