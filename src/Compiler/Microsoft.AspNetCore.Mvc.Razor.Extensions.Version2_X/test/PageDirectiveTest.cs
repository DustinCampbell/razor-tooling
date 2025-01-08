// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.NET.Sdk.Razor.SourceGenerators;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;

public class PageDirectiveTest
{
    [Fact]
    public void TryGetPageDirective_ReturnsTrue_IfPageIsMalformed()
    {
        // Arrange
        var projectEngine = CreateProjectEngine();

        var content = "@page \"some-route-template\" Invalid";
        var sourceDocument = RazorSourceDocument.Create(content, "file");
        var codeDocument = projectEngine.CreateCodeDocument(sourceDocument, FileKinds.Legacy);
        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);

        // Act
        var result = PageDirective.TryGetPageDirective(irDocument, out var pageDirective);

        // Assert
        Assert.True(result);
        Assert.Equal("some-route-template", pageDirective.RouteTemplate);
        Assert.NotNull(pageDirective.DirectiveNode);
    }

    [Fact]
    public void TryGetPageDirective_ReturnsTrue_IfPageIsImported()
    {
        // Arrange
        var projectEngine = CreateProjectEngine();

        var content = "Hello world";
        var sourceDocument = RazorSourceDocument.Create(content, "file");
        var importDocument = RazorSourceDocument.Create("@page", "imports.cshtml");
        var codeDocument = projectEngine.CreateCodeDocument(sourceDocument, [importDocument], FileKinds.Legacy);
        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);

        // Act
        var result = PageDirective.TryGetPageDirective(irDocument, out var pageDirective);

        // Assert
        Assert.True(result);
        Assert.Null(pageDirective.RouteTemplate);
    }

    [Fact]
    public void TryGetPageDirective_ReturnsFalse_IfPageDoesNotHaveDirective()
    {
        // Arrange
        var projectEngine = CreateProjectEngine();

        var content = "Hello world";
        var sourceDocument = RazorSourceDocument.Create(content, "file");
        var codeDocument = projectEngine.CreateCodeDocument(sourceDocument, FileKinds.Legacy);
        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);

        // Act
        var result = PageDirective.TryGetPageDirective(irDocument, out var pageDirective);

        // Assert
        Assert.False(result);
        Assert.Null(pageDirective);
    }

    [Fact]
    public void TryGetPageDirective_ReturnsTrue_IfPageDoesStartWithDirective()
    {
        // Arrange
        var projectEngine = CreateProjectEngine();

        var content = "Hello @page";
        var sourceDocument = RazorSourceDocument.Create(content, "file");
        var codeDocument = projectEngine.CreateCodeDocument(sourceDocument, FileKinds.Legacy);
        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);

        // Act
        var result = PageDirective.TryGetPageDirective(irDocument, out var pageDirective);

        // Assert
        Assert.True(result);
        Assert.Null(pageDirective.RouteTemplate);
        Assert.NotNull(pageDirective.DirectiveNode);
    }

    [Fact]
    public void TryGetPageDirective_ReturnsTrue_IfContentHasDirective()
    {
        // Arrange
        var projectEngine = CreateProjectEngine();

        var content = "@page";
        var sourceDocument = RazorSourceDocument.Create(content, "file");
        var codeDocument = projectEngine.CreateCodeDocument(sourceDocument, FileKinds.Legacy);
        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);

        // Act
        var result = PageDirective.TryGetPageDirective(irDocument, out var pageDirective);

        // Assert
        Assert.True(result);
        Assert.Null(pageDirective.RouteTemplate);
    }

    [Fact]
    public void TryGetPageDirective_ParsesRouteTemplate()
    {
        // Arrange
        var projectEngine = CreateProjectEngine();

        var content = "@page \"some-route-template\"";
        var sourceDocument = RazorSourceDocument.Create(content, "file");
        var codeDocument = projectEngine.CreateCodeDocument(sourceDocument, FileKinds.Legacy);
        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);

        // Act
        var result = PageDirective.TryGetPageDirective(irDocument, out var pageDirective);

        // Assert
        Assert.True(result);
        Assert.Equal("some-route-template", pageDirective.RouteTemplate);
    }

    private static RazorProjectEngine CreateProjectEngine()
    {
        return RazorProjectEngine.Create(b =>
        {
            PageDirective.Register(b);
            b.Features.Add(new ConfigureRazorParserOptions(useRoslynTokenizer: true, CSharpParseOptions.Default));
        });
    }

    private static DocumentIntermediateNode CreateIRDocument(RazorEngine engine, RazorCodeDocument codeDocument)
    {
        foreach (var phase in engine.Phases)
        {
            phase.Execute(codeDocument);

            if (phase is IRazorDocumentClassifierPhase)
            {
                break;
            }
        }

        return codeDocument.GetDocumentIntermediateNode();
    }
}
