// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

public class ExtensionTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Theory]
    [InlineData("x", "x")]
    [InlineData("_Imports.razor", "_Imports.razor")]
    [InlineData("/_Imports.razor", "_Imports.razor")]
    [InlineData("Views/_Imports.razor", @"Views\_Imports.razor")]
    [InlineData("/Views/_Imports.razor", @"Views\_Imports.razor")]
    [InlineData("Components/Pages/Error.razor", @"Components\Pages\Error.razor")]
    [InlineData("/Components/Pages/Error.razor", @"Components\Pages\Error.razor")]
    public void GetTargetPathFromFilePath(string filePath, string expectedTargetPath)
    {
        // Arrange
        var projectItem = new TestRazorProjectItem(filePath);

        // Act
        var targetPath = projectItem.GetTargetPathFromFilePath();

        // Assert
        Assert.Equal(expectedTargetPath, targetPath);
    }

    [Fact]
    public void GetTargetPathFromFilePath_HandlesNull()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem(filePath: null!);

        // Act
        var targetPath = projectItem.GetTargetPathFromFilePath();

        // Assert
        Assert.Null(targetPath);
    }

    [Fact]
    public void GetTargetPathFromFilePath_HandlesSingleSlash()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem(filePath: "/");

        // Act
        var targetPath = projectItem.GetTargetPathFromFilePath();

        // Assert
        Assert.Null(targetPath);
    }
}
