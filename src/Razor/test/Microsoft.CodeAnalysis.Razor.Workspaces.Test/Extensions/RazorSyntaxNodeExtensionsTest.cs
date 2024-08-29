// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.Test;

public class RazorSyntaxNodeExtensionsTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void GetNearestAncestorTagInfo_MarkupElement()
    {
        var node = GetSyntaxNode(
            """
                @addTagHelper *, TestAssembly
                <p><$$strong></strong></p>
                """,
            isRazorFile: false);

        var element = node.FirstAncestorOrSelf<MarkupElementSyntax>();
        Assert.NotNull(element);

        var (ancestorName, ancestorIsTagHelper) = element.GetNearestAncestorTagInfo();

        Assert.Equal("p", ancestorName);
        Assert.False(ancestorIsTagHelper);
    }

    [Fact]
    public void GetNearestAncestorTagInfo_TagHelperElement()
    {
        var node = GetSyntaxNode(
            """
                @addTagHelper *, TestAssembly
                <test1><$$test2></test2></test1>
                """,
            isRazorFile: false);

        var element = node.FirstAncestorOrSelf<MarkupTagHelperElementSyntax>();
        Assert.NotNull(element);

        var (ancestorName, ancestorIsTagHelper) = element.GetNearestAncestorTagInfo();

        Assert.Equal("test1", ancestorName);
        Assert.True(ancestorIsTagHelper);
    }

    private static SyntaxNode GetSyntaxNode(TestCode testCode, bool isRazorFile = true, params ImmutableArray<TagHelperDescriptor> tagHelpers)
    {
        var codeDocument = TestRazorCodeDocumentFactory.Create(testCode.Text, isRazorFile, tagHelpers);
        var syntaxTree = codeDocument.GetSyntaxTree();

        var node = syntaxTree.Root.FindInnermostNode(testCode.Position);
        Assert.NotNull(node);

        return node;
    }

    private static SyntaxNode GetSyntaxNode(TestCode testCode, bool isRazorFile = true)
    {
        return GetSyntaxNode(testCode, isRazorFile, TestTagHelperData.DefaultTagHelpers);
    }
}
