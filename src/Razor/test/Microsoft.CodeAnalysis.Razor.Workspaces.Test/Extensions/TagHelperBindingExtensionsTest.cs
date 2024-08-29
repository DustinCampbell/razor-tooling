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

public class TagHelperBindingExtensionsTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void GetBoundTagHelperAttributes_MatchesPrefixedAttributeName()
    {
        ImmutableArray<TagHelperDescriptor> documentDescriptors =
        [
            TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("a"))
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
                .Build()
        ];

        var expectedAttributeDescriptors = new[]
        {
            documentDescriptors[0].BoundAttributes.Last()
        };

        var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);
        Assert.True(documentContext.TryGetTagHelperBinding(tagName: "a", attributes: [], parentTagName: null, parentIsTagHelper: false, out var binding));

        var tagHelperAttributes = binding.GetBoundTagHelperAttributes("asp-route-something");

        Assert.Equal(expectedAttributeDescriptors, tagHelperAttributes);
    }

    [Fact]
    public void GetBoundTagHelperAttributes_MatchesAttributeName()
    {
        ImmutableArray<TagHelperDescriptor> documentDescriptors =
        [
            TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("input"))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                        .Name("asp-for")
                        .TypeName(typeof(string).FullName)
                        .Metadata(PropertyName("AspFor")))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                        .Name("asp-extra")
                        .TypeName(typeof(string).FullName)
                        .Metadata(PropertyName("AspExtra")))
                .Build()
        ];

        var expectedAttributeDescriptors = new[]
        {
            documentDescriptors[0].BoundAttributes.First()
        };

        var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);
        Assert.True(documentContext.TryGetTagHelperBinding(tagName: "input", attributes: [], parentTagName: null, parentIsTagHelper: false, out var binding));

        var tagHelperAttributes = binding.GetBoundTagHelperAttributes("asp-for");

        Assert.Equal(expectedAttributeDescriptors, tagHelperAttributes);
    }
}
