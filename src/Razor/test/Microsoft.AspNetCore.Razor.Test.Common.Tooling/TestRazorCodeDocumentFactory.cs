// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal static class TestRazorCodeDocumentFactory
{
    private const string CSHtmlFile = "test.cshtml";
    private const string RazorFile = "test.razor";

    public static string GetFileName(bool isRazorFile)
        => isRazorFile ? RazorFile : CSHtmlFile;

    public static RazorCodeDocument Create(string text, bool isRazorFile, params ImmutableArray<TagHelperDescriptor> tagHelpers)
        => Create(text, GetFileName(isRazorFile), tagHelpers);

    public static RazorCodeDocument Create(string text, string filePath, params ImmutableArray<TagHelperDescriptor> tagHelpers)
    {
        tagHelpers = tagHelpers.NullToEmpty();

        var sourceDocument = TestRazorSourceDocument.Create(text, filePath: filePath, relativePath: filePath);
        var projectEngine = RazorProjectEngine.Create(builder => { });
        var fileKind = filePath.EndsWith(".razor", StringComparison.Ordinal) ? FileKinds.Component : FileKinds.Legacy;

        return projectEngine.ProcessDesignTime(sourceDocument, fileKind, importSources: default, tagHelpers);
    }
}
