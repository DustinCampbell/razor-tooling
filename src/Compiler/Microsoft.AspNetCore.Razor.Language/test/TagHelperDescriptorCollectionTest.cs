// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.CommonMetadata;

namespace Microsoft.AspNetCore.Razor.Language;

public class TagHelperDescriptorCollectionTest
{
    [Fact]
    public void EmptyCollection()
    {
        var collection1 = TagHelperDescriptorCollection.Empty;
        Assert.Empty(collection1);

        var collection2 = TagHelperDescriptorCollection.Create();
        Assert.Empty(collection2);

        using var builder = TagHelperDescriptorCollection.GetBuilder();
        var collection3 = builder.ToCollection();
        Assert.Empty(collection3);

        Assert.Same(collection1, collection2);
        Assert.Same(collection1, collection3);
    }

    [Fact]
    public void ListCollection()
    {
        var counterTagHelper = CreateTagHelperDescriptor(
            tagName: "Counter",
            typeName: "CounterTagHelper",
            assemblyName: "Components.Component",
            tagMatchingRuleName: "Input",
            attributes: [
                builder => builder
                    .Name("IncrementBy")
                    .Metadata(PropertyName("IncrementBy"))
                    .TypeName("System.Int32"),
            ]);

        var inputTagHelper = CreateTagHelperDescriptor(
            tagName: "input",
            typeName: "InputTagHelper",
            assemblyName: "TestAssembly",
            tagMatchingRuleName: "Microsoft.AspNetCore.Components.Forms.Input",
            attributes: [
                builder => builder
                    .Name("value")
                    .Metadata(PropertyName("FooProp"))
                    .TypeName("System.String"),
            ]);

        var collection = TagHelperDescriptorCollection.Create([counterTagHelper, inputTagHelper]);

        Assert.Equal(2, collection.Count);
        Assert.Equal(counterTagHelper, collection[0]);
        Assert.Equal(inputTagHelper, collection[1]);
    }

    [Fact]
    public void ListCollectionRemovesDuplicates()
    {
        // Arrange
        var descriptor1 = CreateTagHelperDescriptor(
            tagName: "input",
            typeName: "InputTagHelper",
            assemblyName: "TestAssembly",
            attributes: [
                builder => builder
                    .Name("value")
                    .Metadata(PropertyName("FooProp"))
                    .TypeName("System.String")
            ]);

        var descriptor2 = CreateTagHelperDescriptor(
            tagName: "input",
            typeName: "InputTagHelper",
            assemblyName: "TestAssembly",
            attributes: [
                builder => builder
                    .Name("value")
                    .Metadata(PropertyName("FooProp"))
                    .TypeName("System.String")
            ]);

        var collection = TagHelperDescriptorCollection.Create([descriptor1, descriptor2]);

        var resultDescriptor = Assert.Single(collection);
        Assert.Same(descriptor1, resultDescriptor);
    }

    [Fact]
    public void MergedCollection()
    {
        var counterTagHelper = CreateTagHelperDescriptor(
            tagName: "Counter",
            typeName: "CounterTagHelper",
            assemblyName: "Components.Component",
            tagMatchingRuleName: "Input",
            attributes: [
                builder => builder
                    .Name("IncrementBy")
                    .Metadata(PropertyName("IncrementBy"))
                    .TypeName("System.Int32"),
            ]);

        var inputTagHelper = CreateTagHelperDescriptor(
            tagName: "input",
            typeName: "InputTagHelper",
            assemblyName: "TestAssembly",
            tagMatchingRuleName: "Microsoft.AspNetCore.Components.Forms.Input",
            attributes: [
                builder => builder
                    .Name("value")
                    .Metadata(PropertyName("FooProp"))
                    .TypeName("System.String"),
            ]);

        var collection1 = TagHelperDescriptorCollection.Create([counterTagHelper]);
        var collection2 = TagHelperDescriptorCollection.Create([inputTagHelper]);

        var mergedCollection = TagHelperDescriptorCollection.Merge(collection1, collection2);

        Assert.Equal(2, mergedCollection.Count);
        Assert.Equal(counterTagHelper, mergedCollection[0]);
        Assert.Equal(inputTagHelper, mergedCollection[1]);
    }

    [Fact]
    public void MergedEmptyCollections()
    {
        var mergedCollection = TagHelperDescriptorCollection.Merge(TagHelperDescriptorCollection.Empty, TagHelperDescriptorCollection.Empty);

        Assert.Empty(mergedCollection);
    }

    private static TagHelperDescriptor CreateTagHelperDescriptor(
        string tagName,
        string typeName,
        string assemblyName,
        string? tagMatchingRuleName = null,
        IEnumerable<Action<BoundAttributeDescriptorBuilder>>? attributes = null)
    {
        var builder = TagHelperDescriptorBuilder.Create(typeName, assemblyName);
        builder.Metadata(TypeName(typeName));

        if (attributes != null)
        {
            foreach (var attributeBuilder in attributes)
            {
                builder.BoundAttributeDescriptor(attributeBuilder);
            }
        }

        builder.TagMatchingRuleDescriptor(ruleBuilder => ruleBuilder.RequireTagName(tagMatchingRuleName ?? tagName));

        var descriptor = builder.Build();

        return descriptor;
    }
}
