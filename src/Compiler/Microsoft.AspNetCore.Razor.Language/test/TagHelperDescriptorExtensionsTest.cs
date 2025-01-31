// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Xunit;
using static Microsoft.AspNetCore.Razor.Language.CommonMetadata;

namespace Microsoft.AspNetCore.Razor.Language;

public class TagHelperDescriptorExtensionsTest
{
    [Fact]
    public void GetTypeName_ReturnsTypeName()
    {
        // Arrange
        var expectedTypeName = "TestTagHelper";
        var descriptor = TagHelperDescriptorBuilder.Create(expectedTypeName, "TestAssembly").Metadata(TypeName(expectedTypeName)).Build();

        // Act
        var typeName = descriptor.GetTypeName();

        // Assert
        Assert.Equal(expectedTypeName, typeName);
    }

    [Fact]
    public void GetTypeName_ReturnsNullIfNoTypeName()
    {
        // Arrange
        var descriptor = TagHelperDescriptorBuilder.Create("Test", "TestAssembly").Build();

        // Act
        var typeName = descriptor.GetTypeName();

        // Assert
        Assert.Null(typeName);
    }
}
