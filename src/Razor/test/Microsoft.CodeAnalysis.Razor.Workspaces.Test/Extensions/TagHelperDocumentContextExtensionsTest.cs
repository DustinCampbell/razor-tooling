// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.AspNetCore.Razor.Language.CommonMetadata;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Test.Extensions;

public class TagHelperDocumentContextExtensionsTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void TryGetTagHelperBinding_DoesNotAllowOptOutCharacterPrefix()
    {
        ImmutableArray<TagHelperDescriptor> documentDescriptors =
        [
            TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);

        Assert.False(documentContext.TryGetTagHelperBinding(tagName: "!a", attributes: [], parentTagName: null, parentIsTagHelper: false, out _));
    }

    [Fact]
    public void TryGetTagHelperBinding_WorksAsExpected()
    {
        ImmutableArray<TagHelperDescriptor> documentDescriptors =
        [
            TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                        .RequireTagName("a")
                        .RequireAttributeDescriptor(attribute => attribute.Name("asp-for")))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                        .Name("asp-for")
                        .TypeName(typeof(string).FullName)
                        .Metadata(PropertyName("AspFor")))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                        .Name("asp-route")
                        .TypeName(typeof(IDictionary<string, string>).Namespace + "IDictionary<string, string>")
                        .Metadata(PropertyName("AspRoute"))
                        .AsDictionaryAttribute("asp-route-", typeof(string).FullName))
                .Build(),
            TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("input"))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                        .Name("asp-for")
                        .TypeName(typeof(string).FullName)
                        .Metadata(PropertyName("AspFor")))
                .Build(),
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);
        var attributes = ImmutableArray.Create(
            new KeyValuePair<string, string>("asp-for", "Name"));

        Assert.True(documentContext.TryGetTagHelperBinding(tagName: "a", attributes, parentTagName: "p", parentIsTagHelper: false, out var binding));

        var descriptor = Assert.Single(binding.Descriptors);
        Assert.Equal(documentDescriptors[0], descriptor);
        var boundRule = Assert.Single(binding.Mappings[descriptor]);
        Assert.Equal(documentDescriptors[0].TagMatchingRules.First(), boundRule);
    }

    [Fact]
    public void GetTagHelpersGivenTag_DoesNotAllowOptOutCharacterPrefix()
    {
        ImmutableArray<TagHelperDescriptor> documentDescriptors =
        [
            TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);

        var descriptors = documentContext.GetTagHelpersGivenTag("!strong", parentTag: null);

        Assert.Empty(descriptors);
    }

    [Fact]
    public void GetTagHelpersGivenTag_RequiresTagName()
    {
        ImmutableArray<TagHelperDescriptor> documentDescriptors =
        [
            TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);

        var descriptors = documentContext.GetTagHelpersGivenTag("strong", "p");

        Assert.Equal<TagHelperDescriptor>(documentDescriptors, descriptors);
    }

    [Fact]
    public void GetTagHelpersGivenTag_RestrictsTagHelpersBasedOnTagName()
    {
        ImmutableArray<TagHelperDescriptor> expectedDescriptors =
        [
            TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(
                    rule => rule
                        .RequireTagName("a")
                        .RequireParentTag("div"))
                .Build()
        ];

        ImmutableArray<TagHelperDescriptor> documentDescriptors =
        [
            expectedDescriptors[0],
            TagHelperDescriptorBuilder.Create("TestType2", "TestAssembly")
                .TagMatchingRuleDescriptor(
                    rule => rule
                        .RequireTagName("strong")
                        .RequireParentTag("div"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);

        var descriptors = documentContext.GetTagHelpersGivenTag("a", "div");

        Assert.Equal<TagHelperDescriptor>(expectedDescriptors, descriptors);
    }

    [Fact]
    public void GetTagHelpersGivenTag_RestrictsTagHelpersBasedOnTagHelperPrefix()
    {
        ImmutableArray<TagHelperDescriptor> expectedDescriptors =
        [
            TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong"))
                .Build()
        ];

        ImmutableArray<TagHelperDescriptor> documentDescriptors =
        [
            expectedDescriptors[0],
            TagHelperDescriptorBuilder.Create("TestType2", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("thstrong"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create("th", documentDescriptors);

        var descriptors = documentContext.GetTagHelpersGivenTag("thstrong", "div");

        Assert.Equal<TagHelperDescriptor>(expectedDescriptors, descriptors);
    }

    [Fact]
    public void GetTagHelpersGivenTag_RestrictsTagHelpersBasedOnParent()
    {
        ImmutableArray<TagHelperDescriptor> expectedDescriptors =
        [
            TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(
                    rule => rule
                        .RequireTagName("strong")
                        .RequireParentTag("div"))
                .Build()
        ];

        ImmutableArray<TagHelperDescriptor> documentDescriptors =
        [
            expectedDescriptors[0],
            TagHelperDescriptorBuilder.Create("TestType2", "TestAssembly")
                .TagMatchingRuleDescriptor(
                    rule => rule
                        .RequireTagName("strong")
                        .RequireParentTag("p"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);

        var descriptors = documentContext.GetTagHelpersGivenTag("strong", "div");

        Assert.Equal<TagHelperDescriptor>(expectedDescriptors, descriptors);
    }

    [Fact]
    public void GetTagHelpersGivenParent_AllowsRootParentTag()
    {
        ImmutableArray<TagHelperDescriptor> documentDescriptors =
        [
            TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);

        var descriptors = documentContext.GetTagHelpersGivenParent(parentTag: null /* root */);

        Assert.Equal<TagHelperDescriptor>(documentDescriptors, descriptors);
    }

    [Fact]
    public void GetTagHelpersGivenParent_AllowsRootParentTagForParentRestrictedTagHelperDescriptors()
    {
        ImmutableArray<TagHelperDescriptor> documentDescriptors =
        [
            TagHelperDescriptorBuilder.Create("DivTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .Build(),
            TagHelperDescriptorBuilder.Create("PTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("p")
                    .RequireParentTag("body"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);

        var descriptors = documentContext.GetTagHelpersGivenParent(parentTag: null /* root */);

        var descriptor = Assert.Single(descriptors);
        Assert.Equal(documentDescriptors[0], descriptor);
    }

    [Fact]
    public void GetTagHelpersGivenParent_AllowsUnspecifiedParentTagHelpers()
    {
        ImmutableArray<TagHelperDescriptor> documentDescriptors =
        [
            TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);

        var descriptors = documentContext.GetTagHelpersGivenParent("p");

        Assert.Equal<TagHelperDescriptor>(documentDescriptors, descriptors);
    }

    [Fact]
    public void GetTagHelpersGivenParent_RestrictsTagHelpersBasedOnParent()
    {
        ImmutableArray<TagHelperDescriptor> expectedDescriptors =
        [
            TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(
                    rule => rule
                        .RequireTagName("p")
                        .RequireParentTag("div"))
                .Build()
        ];

        ImmutableArray<TagHelperDescriptor> documentDescriptors =
        [
            expectedDescriptors[0],
            TagHelperDescriptorBuilder.Create("TestType2", "TestAssembly")
                .TagMatchingRuleDescriptor(
                    rule => rule
                        .RequireTagName("strong")
                        .RequireParentTag("p"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);

        var descriptors = documentContext.GetTagHelpersGivenParent("div");

        Assert.Equal<TagHelperDescriptor>(expectedDescriptors, descriptors);
    }
}
