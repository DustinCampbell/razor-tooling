// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Completion;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.AspNetCore.Razor.Language.CommonMetadata;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test;

public class TagHelperFactsServiceTest(ITestOutputHelper testOutput) : TagHelperServiceTestBase(testOutput)
{
    [Fact]
    public void ToAttributePairs_DirectiveAttribute()
    {
        var startTag = GetStartTag<MarkupTagHelperStartTagSyntax>($"<Te$$stElement @test='abc' />");

        var attributePairs = startTag.Attributes.ToAttributePairs();

        var attribute = Assert.Single(attributePairs);
        Assert.Equal("@test", attribute.Key);
        Assert.Equal("abc", attribute.Value);
    }

    [Fact]
    public void ToAttributePairs_DirectiveAttributeWithParameter()
    {
        var startTag = GetStartTag<MarkupTagHelperStartTagSyntax>($"<Te$$stElement @test:something='abc' />");

        var attributePairs = startTag.Attributes.ToAttributePairs();

        var attribute = Assert.Single(attributePairs);
        Assert.Equal("@test:something", attribute.Key);
        Assert.Equal("abc", attribute.Value);
    }

    [Fact]
    public void ToAttributePairs_MinimizedDirectiveAttribute()
    {
        var startTag = GetStartTag<MarkupTagHelperStartTagSyntax>($"<Te$$stElement @minimized />");

        var attributePairs = startTag.Attributes.ToAttributePairs();

        var attribute = Assert.Single(attributePairs);
        Assert.Equal("@minimized", attribute.Key);
        Assert.Equal(string.Empty, attribute.Value);
    }

    [Fact]
    public void ToAttributePairs_MinimizedDirectiveAttributeWithParameter()
    {
        var startTag = GetStartTag<MarkupTagHelperStartTagSyntax>($"<Te$$stElement @minimized:something />");

        var attributePairs = startTag.Attributes.ToAttributePairs();

        var attribute = Assert.Single(attributePairs);
        Assert.Equal("@minimized:something", attribute.Key);
        Assert.Equal(string.Empty, attribute.Value);
    }

    [Fact]
    public void ToAttributePairs_TagHelperAttribute()
    {
        var tagHelper = TagHelperDescriptorBuilder.Create("WithBoundAttribute", "TestAssembly");
        tagHelper.TagMatchingRule(rule => rule.TagName = "test");
        tagHelper.BindAttribute(attribute =>
        {
            attribute.Name = "bound";
            attribute.SetMetadata(PropertyName("Bound"));
            attribute.TypeName = typeof(bool).FullName;
        });

        tagHelper.SetMetadata(TypeName("WithBoundAttribute"));

        var startTag = GetStartTag<MarkupTagHelperStartTagSyntax>("""
            @addTagHelper *, TestAssembly
            <t$$est bound='true' />
            """,
            isRazorFile: false,
            tagHelper.Build());

        var attributePairs = startTag.Attributes.ToAttributePairs();

        var attribute = Assert.Single(attributePairs);
        Assert.Equal("bound", attribute.Key);
        Assert.Equal("true", attribute.Value);
    }

    [Fact]
    public void ToAttributePairs_MinimizedTagHelperAttribute()
    {
        var tagHelper = TagHelperDescriptorBuilder.Create("WithBoundAttribute", "TestAssembly");
        tagHelper.TagMatchingRule(rule => rule.TagName = "test");
        tagHelper.BindAttribute(attribute =>
        {
            attribute.Name = "bound";
            attribute.SetMetadata(PropertyName("Bound"));
            attribute.TypeName = typeof(bool).FullName;
        });
        tagHelper.SetMetadata(TypeName("WithBoundAttribute"));

        var startTag = GetStartTag<MarkupTagHelperStartTagSyntax>("""
            @addTagHelper *, TestAssembly
            <t$$est bound />
            """,
            isRazorFile: false,
            tagHelper.Build());

        var attributePairs = startTag.Attributes.ToAttributePairs();

        var attribute = Assert.Single(attributePairs);
        Assert.Equal("bound", attribute.Key);
        Assert.Equal(string.Empty, attribute.Value);
    }

    [Fact]
    public void ToAttributePairs_UnboundAttribute()
    {
        var startTag = GetStartTag<MarkupStartTagSyntax>("""
            @addTagHelper *, TestAssembly
            <i$$nput unbound='hello world' />
            """,
            isRazorFile: false);

        var attributePairs = startTag.Attributes.ToAttributePairs();

        var attribute = Assert.Single(attributePairs);
        Assert.Equal("unbound", attribute.Key);
        Assert.Equal("hello world", attribute.Value);
    }

    [Fact]
    public void ToAttributePairs_UnboundMinimizedAttribute()
    {
        var startTag = GetStartTag<MarkupStartTagSyntax>("""
            @addTagHelper *, TestAssembly
            <i$$nput unbound />
            """,
            isRazorFile: false);

        var attributePairs = startTag.Attributes.ToAttributePairs();

        var attribute = Assert.Single(attributePairs);
        Assert.Equal("unbound", attribute.Key);
        Assert.Equal(string.Empty, attribute.Value);
    }

    [Fact]
    public void ToAttributePairs_IgnoresMiscContent()
    {
        var startTag = GetStartTag<MarkupStartTagSyntax>("""
            @addTagHelper *, TestAssembly
            <i$$nput unbound @DateTime.Now />
            """,
            isRazorFile: false);

        var attributePairs = startTag.Attributes.ToAttributePairs();

        var attribute = Assert.Single(attributePairs);
        Assert.Equal("unbound", attribute.Key);
        Assert.Equal(string.Empty, attribute.Value);
    }

    private static TNode GetStartTag<TNode>(TestCode testCode, bool isRazorFile = true, params ImmutableArray<TagHelperDescriptor> tagHelpers)
        where TNode : class, IStartTagSyntaxNode
    {
        var codeDocument = CreateCodeDocument(testCode.Text, isRazorFile, tagHelpers);
        var syntaxTree = codeDocument.GetSyntaxTree();

        var node = syntaxTree.Root.FindInnermostNode(testCode.Position) as TNode;
        Assert.NotNull(node);

        return node;
    }

    private static TNode GetStartTag<TNode>(TestCode testCode, bool isRazorFile = true)
        where TNode : class, IStartTagSyntaxNode
    {
        return GetStartTag<TNode>(testCode, isRazorFile, DefaultTagHelpers);
    }
}
