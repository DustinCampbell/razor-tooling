// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public class InjectDirectiveTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Version_3_0;

    [Fact]
    public void InjectDirectivePass_Execute_DefinesProperty()
    {
        // Arrange
        var projectEngine = CreateProjectEngine();

        var source = RazorSourceDocument.Create(@"
@inject PropertyType PropertyName
", "test.cshtml");

        var codeDocument = projectEngine.CreateCodeDocument(source, FileKinds.Legacy);

        var pass = new InjectDirective.Pass()
        {
            Engine = projectEngine.Engine,
        };

        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);

        // Act
        pass.Execute(codeDocument, irDocument);

        // Assert
        var @class = FindClassNode(irDocument);
        Assert.NotNull(@class);
        Assert.Equal(2, @class.Children.Count);

        var node = Assert.IsType<InjectIntermediateNode>(@class.Children[1]);
        Assert.Equal("PropertyType", node.TypeName);
        Assert.Equal("PropertyName", node.MemberName);
    }

    [Fact]
    public void InjectDirectivePass_Execute_DedupesPropertiesByName()
    {
        // Arrange
        var projectEngine = CreateProjectEngine();

        var source = RazorSourceDocument.Create(@"
@inject PropertyType PropertyName
@inject PropertyType2 PropertyName
", "test.cshtml");

        var codeDocument = projectEngine.CreateCodeDocument(source, FileKinds.Legacy);

        var pass = new InjectDirective.Pass()
        {
            Engine = projectEngine.Engine,
        };

        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);

        // Act
        pass.Execute(codeDocument, irDocument);

        // Assert
        var @class = FindClassNode(irDocument);
        Assert.NotNull(@class);
        Assert.Equal(2, @class.Children.Count);

        var node = Assert.IsType<InjectIntermediateNode>(@class.Children[1]);
        Assert.Equal("PropertyType2", node.TypeName);
        Assert.Equal("PropertyName", node.MemberName);
    }

    [Fact]
    public void InjectDirectivePass_Execute_ExpandsTModel_WithDynamic()
    {
        // Arrange
        var projectEngine = CreateProjectEngine();

        var source = RazorSourceDocument.Create(@"
@inject PropertyType<TModel> PropertyName
", "test.cshtml");

        var codeDocument = projectEngine.CreateCodeDocument(source, FileKinds.Legacy);

        var pass = new InjectDirective.Pass()
        {
            Engine = projectEngine.Engine,
        };

        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);

        // Act
        pass.Execute(codeDocument, irDocument);

        // Assert
        var @class = FindClassNode(irDocument);
        Assert.NotNull(@class);
        Assert.Equal(2, @class.Children.Count);

        var node = Assert.IsType<InjectIntermediateNode>(@class.Children[1]);
        Assert.Equal("PropertyType<dynamic>", node.TypeName);
        Assert.Equal("PropertyName", node.MemberName);
    }

    [Fact]
    public void InjectDirectivePass_Execute_ExpandsTModel_WithModelTypeFirst()
    {
        // Arrange
        var projectEngine = CreateProjectEngine();

        var source = RazorSourceDocument.Create(@"
@model ModelType
@inject PropertyType<TModel> PropertyName
", "test.cshtml");

        var codeDocument = projectEngine.CreateCodeDocument(source, FileKinds.Legacy);

        var pass = new InjectDirective.Pass()
        {
            Engine = projectEngine.Engine,
        };

        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);

        // Act
        pass.Execute(codeDocument, irDocument);

        // Assert
        var @class = FindClassNode(irDocument);
        Assert.NotNull(@class);
        Assert.Equal(2, @class.Children.Count);

        var node = Assert.IsType<InjectIntermediateNode>(@class.Children[1]);
        Assert.Equal("PropertyType<ModelType>", node.TypeName);
        Assert.Equal("PropertyName", node.MemberName);
    }

    [Fact]
    public void InjectDirectivePass_Execute_ExpandsTModel_WithModelType()
    {
        // Arrange
        var projectEngine = CreateProjectEngine();

        var source = RazorSourceDocument.Create(@"
@inject PropertyType<TModel> PropertyName
@model ModelType
", "test.cshtml");

        var codeDocument = projectEngine.CreateCodeDocument(source, FileKinds.Legacy);

        var pass = new InjectDirective.Pass()
        {
            Engine = projectEngine.Engine,
        };

        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);

        // Act
        pass.Execute(codeDocument, irDocument);

        // Assert
        var @class = FindClassNode(irDocument);
        Assert.NotNull(@class);
        Assert.Equal(2, @class.Children.Count);

        var node = Assert.IsType<InjectIntermediateNode>(@class.Children[1]);
        Assert.Equal("PropertyType<ModelType>", node.TypeName);
        Assert.Equal("PropertyName", node.MemberName);
    }

    private static ClassDeclarationIntermediateNode FindClassNode(IntermediateNode node)
    {
        var visitor = new ClassNodeVisitor();
        visitor.Visit(node);
        return visitor.Node;
    }

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
        // Notice we're not registering the InjectDirective.Pass here so we can run it on demand.
        builder.AddDirective(InjectDirective.Directive);
        builder.AddDirective(ModelDirective.Directive);

        builder.Features.Add(new RazorPageDocumentClassifierPass());
        builder.Features.Add(new MvcViewDocumentClassifierPass());
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

    private class ClassNodeVisitor : IntermediateNodeWalker
    {
        public ClassDeclarationIntermediateNode Node { get; set; }

        public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
        {
            Node = node;
        }
    }
}
