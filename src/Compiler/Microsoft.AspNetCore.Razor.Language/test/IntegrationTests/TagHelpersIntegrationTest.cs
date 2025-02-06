// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.CommonMetadata;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public class TagHelpersIntegrationTest() : IntegrationTestBase(layer: TestProject.Layer.Compiler)
{
    [Fact]
    public void SimpleTagHelpers()
    {
        // Arrange
        var descriptors = new[]
        {
            CreateTagHelperDescriptor(
                tagName: "input",
                typeName: "InputTagHelper",
                assemblyName: "TestAssembly")
        };

        var projectEngine = CreateProjectEngine(builder => builder.AddTagHelpers(descriptors));
        var projectItem = CreateProjectItemFromFile();

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(codeDocument.GetDocumentIntermediateNode());
    }

    [Fact]
    public void TagHelpersWithBoundAttributes()
    {
        // Arrange
        var descriptors = new[]
        {
            CreateTagHelperDescriptor(
                tagName: "input",
                typeName: "InputTagHelper",
                assemblyName: "TestAssembly",
                attributes: [
                    attribute => attribute
                        .Name("bound")
                        .PropertyName("FooProp")
                        .TypeName("System.String")
                ])
        };

        var projectEngine = CreateProjectEngine(builder => builder.AddTagHelpers(descriptors));
        var projectItem = CreateProjectItemFromFile();

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(codeDocument.GetDocumentIntermediateNode());
    }

    [Fact]
    public void NestedTagHelpers()
    {
        // Arrange
        var descriptors = new[]
        {
            CreateTagHelperDescriptor(
                tagName: "p",
                typeName: "PTagHelper",
                assemblyName: "TestAssembly"),
            CreateTagHelperDescriptor(
                tagName: "form",
                typeName: "FormTagHelper",
                assemblyName: "TestAssembly"),
            CreateTagHelperDescriptor(
                tagName: "input",
                typeName: "InputTagHelper",
                assemblyName: "TestAssembly",
                attributes: [
                    attribute => attribute
                        .Name("value")
                        .PropertyName("FooProp")
                        .TypeName("System.String")
                ])
        };

        var projectEngine = CreateProjectEngine(builder => builder.AddTagHelpers(descriptors));
        var projectItem = CreateProjectItemFromFile();

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        var syntaxTree = codeDocument.GetSyntaxTree();
        var irTree = codeDocument.GetDocumentIntermediateNode();
        AssertDocumentNodeMatchesBaseline(codeDocument.GetDocumentIntermediateNode());
    }

    private static TagHelperDescriptor CreateTagHelperDescriptor(
        string tagName,
        string typeName,
        string assemblyName,
        ReadOnlySpan<Action<BoundAttributeDescriptorBuilder>> attributes = default)
    {
        var builder = TagHelperDescriptorBuilder.Create(typeName, assemblyName);
        builder.Metadata(TypeName(typeName));

        foreach (var attributeBuilder in attributes)
        {
            builder.BoundAttributeDescriptor(attributeBuilder);
        }

        builder.TagMatchingRuleDescriptor(ruleBuilder => ruleBuilder.RequireTagName(tagName));

        var descriptor = builder.Build();

        return descriptor;
    }
}
