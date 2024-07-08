// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor;

public class DefaultTagHelperDescriptorProviderTest
{
    private static readonly Assembly _assembly = typeof(DefaultTagHelperDescriptorProviderTest).GetTypeInfo().Assembly;

    [Fact]
    public void Execute_DoesNotAddEditorBrowsableNeverDescriptorsAtDesignTime()
    {
        // Arrange
        var editorBrowsableTypeName = "Microsoft.CodeAnalysis.Razor.Workspaces.Test.EditorBrowsableTagHelper";
        var compilation = TestCompilation.Create(_assembly);
        var descriptorProvider = new DefaultTagHelperDescriptorProvider();

        using var context = TagHelperDescriptorProviderContext.Create(compilation, excludeHidden: true);

        // Act
        descriptorProvider.Execute(context);

        // Assert
        Assert.NotNull(compilation.GetTypeByMetadataName(editorBrowsableTypeName));
        var results = context.Results.ToCollection();
        var nullDescriptors = results.Where(descriptor => descriptor == null);
        Assert.Empty(nullDescriptors);
        var editorBrowsableDescriptor = results.Where(descriptor => descriptor.GetTypeName() == editorBrowsableTypeName);
        Assert.Empty(editorBrowsableDescriptor);
    }

    [Fact]
    public void Execute_WithDefaultDiscoversTagHelpersFromAssemblyAndReference()
    {
        // Arrange
        var testTagHelper = "TestAssembly.TestTagHelper";
        var enumTagHelper = "Microsoft.CodeAnalysis.Razor.Workspaces.Test.EnumTagHelper";
        var csharp = @"
using Microsoft.AspNetCore.Razor.TagHelpers;
namespace TestAssembly
{
    public class TestTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output) {}
    }
}";
        var compilation = TestCompilation.Create(_assembly, CSharpSyntaxTree.ParseText(csharp));
        var descriptorProvider = new DefaultTagHelperDescriptorProvider();

        using var context = TagHelperDescriptorProviderContext.Create(compilation);

        // Act
        descriptorProvider.Execute(context);

        // Assert
        Assert.NotNull(compilation.GetTypeByMetadataName(testTagHelper));
        var results = context.Results.ToCollection();
        Assert.NotEmpty(results);
        Assert.NotEmpty(results.Where(f => f.GetTypeName() == testTagHelper));
        Assert.NotEmpty(results.Where(f => f.GetTypeName() == enumTagHelper));
    }

    [Fact]
    public void Execute_WithTargetAssembly_Works()
    {
        // Arrange
        var testTagHelper = "TestAssembly.TestTagHelper";
        var enumTagHelper = "Microsoft.CodeAnalysis.Razor.Workspaces.Test.EnumTagHelper";
        var csharp = @"
using Microsoft.AspNetCore.Razor.TagHelpers;
namespace TestAssembly
{
    public class TestTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output) {}
    }
}";
        var compilation = TestCompilation.Create(_assembly, CSharpSyntaxTree.ParseText(csharp));
        var descriptorProvider = new DefaultTagHelperDescriptorProvider();

        var targetSymbol = (IAssemblySymbol?)compilation.GetAssemblyOrModuleSymbol(
            compilation.References.First(r => r.Display is string display && display.Contains("Microsoft.CodeAnalysis.Razor.Test.dll")));
        using var context = TagHelperDescriptorProviderContext.Create(compilation, targetSymbol);

        // Act
        descriptorProvider.Execute(context);

        // Assert
        Assert.NotNull(compilation.GetTypeByMetadataName(testTagHelper));
        var results = context.Results.ToCollection();
        Assert.NotEmpty(results);
        Assert.Empty(results.Where(f => f.GetTypeName() == testTagHelper));
        Assert.NotEmpty(results.Where(f => f.GetTypeName() == enumTagHelper));
    }
}
