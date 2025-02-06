// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using static Microsoft.AspNetCore.Razor.Language.CommonMetadata;

namespace Microsoft.AspNetCore.Razor.Language;

public class BoundAttributeDescriptorExtensionsTest
{
    [Fact]
    public void ExpectsStringValue_ReturnsTrue_ForStringProperty()
    {
        // Arrange
        var tagHelperBuilder = new TagHelperDescriptorBuilder(TagHelperKind.Default, "TestTagHelper", "Test");
        tagHelperBuilder.Metadata(TypeName("TestTagHelper"));

        var builder = new BoundAttributeDescriptorBuilder(tagHelperBuilder)
            .Name("test")
            .PropertyName("BoundProp")
            .TypeName(typeof(string).FullName);

        var descriptor = builder.Build();

        // Act
        var result = descriptor.ExpectsStringValue("test");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ExpectsStringValue_ReturnsFalse_ForNonStringProperty()
    {
        // Arrange
        var tagHelperBuilder = new TagHelperDescriptorBuilder(TagHelperKind.Default, "TestTagHelper", "Test");
        tagHelperBuilder.Metadata(TypeName("TestTagHelper"));

        var builder = new BoundAttributeDescriptorBuilder(tagHelperBuilder)
            .Name("test")
            .PropertyName("BoundProp")
            .TypeName(typeof(bool).FullName);

        var descriptor = builder.Build();

        // Act
        var result = descriptor.ExpectsStringValue("test");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ExpectsStringValue_ReturnsTrue_StringIndexerAndNameMatch()
    {
        // Arrange
        var tagHelperBuilder = new TagHelperDescriptorBuilder(TagHelperKind.Default, "TestTagHelper", "Test");
        tagHelperBuilder.Metadata(TypeName("TestTagHelper"));

        var builder = new BoundAttributeDescriptorBuilder(tagHelperBuilder)
            .Name("test")
            .PropertyName("BoundProp")
            .TypeName("System.Collection.Generic.IDictionary<string, string>")
            .AsDictionaryAttribute("prefix-test-", typeof(string).FullName);

        var descriptor = builder.Build();

        // Act
        var result = descriptor.ExpectsStringValue("prefix-test-key");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ExpectsStringValue_ReturnsFalse_StringIndexerAndNameMismatch()
    {
        // Arrange
        var tagHelperBuilder = new TagHelperDescriptorBuilder(TagHelperKind.Default, "TestTagHelper", "Test");
        tagHelperBuilder.Metadata(TypeName("TestTagHelper"));

        var builder = new BoundAttributeDescriptorBuilder(tagHelperBuilder)
            .Name("test")
            .PropertyName("BoundProp")
            .TypeName("System.Collection.Generic.IDictionary<string, string>")
            .AsDictionaryAttribute("prefix-test-", typeof(string).FullName);

        var descriptor = builder.Build();

        // Act
        var result = descriptor.ExpectsStringValue("test");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ExpectsBooleanValue_ReturnsTrue_ForBooleanProperty()
    {
        // Arrange
        var tagHelperBuilder = new TagHelperDescriptorBuilder(TagHelperKind.Default, "TestTagHelper", "Test");
        tagHelperBuilder.Metadata(TypeName("TestTagHelper"));

        var builder = new BoundAttributeDescriptorBuilder(tagHelperBuilder)
            .Name("test")
            .PropertyName("BoundProp")
            .TypeName(typeof(bool).FullName);

        var descriptor = builder.Build();

        // Act
        var result = descriptor.ExpectsBooleanValue("test");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ExpectsBooleanValue_ReturnsFalse_ForNonBooleanProperty()
    {
        // Arrange
        var tagHelperBuilder = new TagHelperDescriptorBuilder(TagHelperKind.Default, "TestTagHelper", "Test");
        tagHelperBuilder.Metadata(TypeName("TestTagHelper"));

        var builder = new BoundAttributeDescriptorBuilder(tagHelperBuilder)
            .Name("test")
            .PropertyName("BoundProp")
            .TypeName(typeof(int).FullName);

        var descriptor = builder.Build();

        // Act
        var result = descriptor.ExpectsBooleanValue("test");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ExpectsBooleanValue_ReturnsTrue_BooleanIndexerAndNameMatch()
    {
        // Arrange
        var tagHelperBuilder = new TagHelperDescriptorBuilder(TagHelperKind.Default, "TestTagHelper", "Test");
        tagHelperBuilder.Metadata(TypeName("TestTagHelper"));

        var builder = new BoundAttributeDescriptorBuilder(tagHelperBuilder)
            .Name("test")
            .PropertyName("BoundProp")
            .TypeName("System.Collection.Generic.IDictionary<string, bool>")
            .AsDictionaryAttribute("prefix-test-", typeof(bool).FullName);

        var descriptor = builder.Build();

        // Act
        var result = descriptor.ExpectsBooleanValue("prefix-test-key");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ExpectsBooleanValue_ReturnsFalse_BooleanIndexerAndNameMismatch()
    {
        // Arrange
        var tagHelperBuilder = new TagHelperDescriptorBuilder(TagHelperKind.Default, "TestTagHelper", "Test");
        tagHelperBuilder.Metadata(TypeName("TestTagHelper"));

        var builder = new BoundAttributeDescriptorBuilder(tagHelperBuilder)
            .Name("test")
            .PropertyName("BoundProp")
            .TypeName("System.Collection.Generic.IDictionary<string, bool>")
            .AsDictionaryAttribute("prefix-test-", typeof(bool).FullName);

        var descriptor = builder.Build();

        // Act
        var result = descriptor.ExpectsBooleanValue("test");

        // Assert
        Assert.False(result);
    }
}
