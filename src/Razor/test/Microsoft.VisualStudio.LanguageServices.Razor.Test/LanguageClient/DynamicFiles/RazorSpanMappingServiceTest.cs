// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Workspaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.DynamicFiles;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.DynamicFiles;

public class RazorSpanMappingServiceTest(ITestOutputHelper testOutput) : WorkspaceTestBase(testOutput)
{
    private readonly HostProject _hostProject = TestProjectData.SomeProject;
    private readonly HostDocument _hostDocument = TestProjectData.SomeProjectFile1;

    [Fact]
    public async Task TryGetMappedSpans_SpanMatchesSourceMapping_ReturnsTrue()
    {
        // Arrange
        var textLoader = RazorTextLoader.Create("""
            @SomeProperty
            """,
            VersionStamp.Create());

        var project = new ProjectSnapshot(
            ProjectState
                .Create(ProjectEngineFactoryProvider, _hostProject, ProjectWorkspaceState.Default)
                .WithAddedHostDocument(_hostDocument, textLoader));

        var document = project.GetDocument(_hostDocument.FilePath);
        Assert.NotNull(document);

        var output = await document.GetGeneratedOutputAsync();
        var generated = output.GetCSharpDocument();

        var symbol = "SomeProperty";
        var span = new TextSpan(generated.GeneratedCode.IndexOf(symbol, StringComparison.Ordinal), symbol.Length);

        // Act
        var result = RazorSpanMappingService.TryGetMappedSpans(span, await document.GetTextAsync(), generated, out var mappedLinePositionSpan, out var mappedSpan);

        // Assert
        Assert.True(result);
        Assert.Equal(new LinePositionSpan(new LinePosition(0, 1), new LinePosition(0, 13)), mappedLinePositionSpan);
        Assert.Equal(new TextSpan(1, symbol.Length), mappedSpan);
    }

    [Fact]
    public async Task TryGetMappedSpans_SpanMatchesSourceMappingAndPosition_ReturnsTrue()
    {
        // Arrange
        var textLoader = RazorTextLoader.Create("""
            @SomeProperty
            @SomeProperty
            @SomeProperty
            """,
            VersionStamp.Create());

        var project = new ProjectSnapshot(
            ProjectState
                .Create(ProjectEngineFactoryProvider, _hostProject, ProjectWorkspaceState.Default)
                .WithAddedHostDocument(_hostDocument, textLoader));

        var document = project.GetDocument(_hostDocument.FilePath);
        Assert.NotNull(document);

        var output = await document.GetGeneratedOutputAsync();
        var generated = output.GetCSharpDocument();

        var symbol = "SomeProperty";
        // Second occurrence
        var span = new TextSpan(generated.GeneratedCode.IndexOf(symbol, generated.GeneratedCode.IndexOf(symbol, StringComparison.Ordinal) + symbol.Length, StringComparison.Ordinal), symbol.Length);

        // Act
        var result = RazorSpanMappingService.TryGetMappedSpans(span, await document.GetTextAsync(), generated, out var mappedLinePositionSpan, out var mappedSpan);

        // Assert
        Assert.True(result);
        Assert.Equal(new LinePositionSpan(new LinePosition(1, 1), new LinePosition(1, 13)), mappedLinePositionSpan);
        Assert.Equal(new TextSpan(1 + symbol.Length + Environment.NewLine.Length + 1, symbol.Length), mappedSpan);
    }

    [Fact]
    public async Task TryGetMappedSpans_SpanWithinSourceMapping_ReturnsTrue()
    {
        // Arrange
        var textLoader = RazorTextLoader.Create("""
            @{
                var x = SomeClass.SomeProperty;
            }
            """,
            VersionStamp.Default);

        var project = new ProjectSnapshot(
            ProjectState
                .Create(ProjectEngineFactoryProvider, _hostProject, ProjectWorkspaceState.Default)
                .WithAddedHostDocument(_hostDocument, textLoader));

        var document = project.GetDocument(_hostDocument.FilePath);
        Assert.NotNull(document);

        var output = await document.GetGeneratedOutputAsync();
        var generated = output.GetCSharpDocument();

        var symbol = "SomeProperty";
        var span = new TextSpan(generated.GeneratedCode.IndexOf(symbol, StringComparison.Ordinal), symbol.Length);

        // Act
        var result = RazorSpanMappingService.TryGetMappedSpans(span, await document.GetTextAsync(), generated, out var mappedLinePositionSpan, out var mappedSpan);

        // Assert
        Assert.True(result);
        Assert.Equal(new LinePositionSpan(new LinePosition(1, 22), new LinePosition(1, 34)), mappedLinePositionSpan);
        Assert.Equal(new TextSpan(2 + Environment.NewLine.Length + "    var x = SomeClass.".Length, symbol.Length), mappedSpan);
    }

    [Fact]
    public async Task TryGetMappedSpans_SpanOutsideSourceMapping_ReturnsFalse()
    {
        // Arrange
        var textLoader = RazorTextLoader.Create("""
            @{
                var x = SomeClass.SomeProperty;
            }
            """,
            VersionStamp.Default);

        var project = new ProjectSnapshot(
            ProjectState
                .Create(ProjectEngineFactoryProvider, _hostProject, ProjectWorkspaceState.Default)
                .WithAddedHostDocument(_hostDocument, textLoader));

        var document = project.GetDocument(_hostDocument.FilePath);
        Assert.NotNull(document);

        var output = await document.GetGeneratedOutputAsync();
        var generated = output.GetCSharpDocument();

        var symbol = "ExecuteAsync";
        var span = new TextSpan(generated.GeneratedCode.IndexOf(symbol, StringComparison.Ordinal), symbol.Length);

        // Act
        var result = RazorSpanMappingService.TryGetMappedSpans(span, await document.GetTextAsync(), generated, out _, out _);

        // Assert
        Assert.False(result);
    }
}
