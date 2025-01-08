// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;

public class ModelDirectiveTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Version_2_1;

    [Fact]
    public void ModelDirective_GetModelType_GetsTypeFromFirstWellFormedDirective()
    {
        // Arrange
        var projectEngine = CreateRuntimeEngine();

        var source = RazorSourceDocument.Create(@"
@model Type1
@model Type2
@model
",
"test.cshtml");
        var codeDocument = projectEngine.CreateCodeDocument(source, FileKinds.Legacy);

        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);

        // Act
        var result = ModelDirective.GetModelType(irDocument);

        // Assert
        Assert.Equal("Type1", result);
    }

    [Fact]
    public void ModelDirective_GetModelType_DefaultsToDynamic()
    {
        // Arrange
        var projectEngine = CreateRuntimeEngine();

        var source = RazorSourceDocument.Create(@" ", "test.cshtml");
        var codeDocument = projectEngine.CreateCodeDocument(source, FileKinds.Legacy);

        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);

        // Act
        var result = ModelDirective.GetModelType(irDocument);

        // Assert
        Assert.Equal("dynamic", result);
    }

    [Fact]
    public void ModelDirectivePass_Execute_ReplacesTModelInBaseType()
    {
        // Arrange
        var projectEngine = CreateRuntimeEngine();

        var source = RazorSourceDocument.Create(@"
@inherits BaseType<TModel>
@model Type1
",
"test.cshtml");
        var codeDocument = projectEngine.CreateCodeDocument(source, FileKinds.Legacy);

        var pass = new ModelDirective.Pass()
        {
            Engine = projectEngine.Engine,
        };

        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);

        // Act
        pass.Execute(codeDocument, irDocument);

        // Assert
        var @class = FindClassNode(irDocument);
        var baseType = @class.BaseType;

        Assert.Equal("BaseType", baseType.BaseType.Content);
        Assert.NotNull(baseType.BaseType.Source);

        Assert.Equal("Type1", baseType.ModelType.Content);
        Assert.NotNull(baseType.ModelType.Source);
    }

    [Fact]
    public void ModelDirectivePass_Execute_ReplacesTModelInBaseType_DifferentOrdering()
    {
        // Arrange
        var projectEngine = CreateRuntimeEngine();

        var source = RazorSourceDocument.Create(@"
@model Type1
@inherits BaseType<TModel>
@model Type2
",
"test.cshtml");
        var codeDocument = projectEngine.CreateCodeDocument(source, FileKinds.Legacy);

        var pass = new ModelDirective.Pass()
        {
            Engine = projectEngine.Engine,
        };

        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);

        // Act
        pass.Execute(codeDocument, irDocument);

        // Assert
        var @class = FindClassNode(irDocument);
        var baseType = @class.BaseType;

        Assert.Equal("BaseType", baseType.BaseType.Content);
        Assert.NotNull(baseType.BaseType.Source);

        Assert.Equal("Type1", baseType.ModelType.Content);
        Assert.NotNull(baseType.ModelType.Source);
    }

    [Fact]
    public void ModelDirectivePass_Execute_NoOpWithoutTModel()
    {
        // Arrange
        var projectEngine = CreateRuntimeEngine();

        var source = RazorSourceDocument.Create(@"
@inherits BaseType
@model Type1
",
"test.cshtml");
        var codeDocument = projectEngine.CreateCodeDocument(source, FileKinds.Legacy);

        var pass = new ModelDirective.Pass()
        {
            Engine = projectEngine.Engine,
        };

        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);

        // Act
        pass.Execute(codeDocument, irDocument);

        // Assert
        var @class = FindClassNode(irDocument);
        Assert.NotNull(@class);
        var baseType = @class.BaseType;

        Assert.Equal("BaseType", baseType.BaseType.Content);
        Assert.NotNull(baseType.BaseType.Source);

        // ISSUE: https://github.com/dotnet/razor/issues/10987 we don't issue a warning or emit anything for the unused model
        Assert.Null(baseType.ModelType);
    }

    [Fact]
    public void ModelDirectivePass_Execute_ReplacesTModelInBaseType_DefaultDynamic()
    {
        // Arrange
        var projectEngine = CreateRuntimeEngine();

        var source = RazorSourceDocument.Create(@"
@inherits BaseType<TModel>
",
"test.cshtml");
        var codeDocument = projectEngine.CreateCodeDocument(source, FileKinds.Legacy);

        var pass = new ModelDirective.Pass()
        {
            Engine = projectEngine.Engine,
        };

        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);

        // Act
        pass.Execute(codeDocument, irDocument);

        // Assert
        var @class = FindClassNode(irDocument);
        Assert.NotNull(@class);
        var baseType = @class.BaseType;

        Assert.Equal("BaseType", baseType.BaseType.Content);
        Assert.NotNull(baseType.BaseType.Source);

        Assert.Equal("dynamic", baseType.ModelType.Content);
        Assert.Null(baseType.ModelType.Source);
    }

    [Fact]
    public void ModelDirectivePass_DesignTime_AddsTModelUsingDirective()
    {
        // Arrange
        var projectEngine = CreateDesignTimeEngine();

        var source = RazorSourceDocument.Create(@"
@inherits BaseType<TModel>
",
"test.cshtml");
        var codeDocument = projectEngine.CreateCodeDocument(source, FileKinds.Legacy);

        var pass = new ModelDirective.Pass()
        {
            Engine = projectEngine.Engine,
        };

        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);

        // Act
        pass.Execute(codeDocument, irDocument);

        // Assert
        var @class = FindClassNode(irDocument);
        Assert.NotNull(@class);
        var baseType = @class.BaseType;

        Assert.Equal("BaseType", baseType.BaseType.Content);
        Assert.Null(baseType.BaseType.Source);

        Assert.Equal("dynamic", baseType.ModelType.Content);
        Assert.Null(baseType.ModelType.Source);

        var @namespace = FindNamespaceNode(irDocument);
        var usingNode = Assert.IsType<UsingDirectiveIntermediateNode>(@namespace.Children[0]);
        Assert.Equal($"TModel = global::{typeof(object).FullName}", usingNode.Content);
    }

    [Fact]
    public void ModelDirectivePass_DesignTime_WithModel_AddsTModelUsingDirective()
    {
        // Arrange
        var projectEngine = CreateDesignTimeEngine();

        var source = RazorSourceDocument.Create(@"
@inherits BaseType<TModel>
@model SomeType
",
"test.cshtml");
        var codeDocument = projectEngine.CreateCodeDocument(source, FileKinds.Legacy);

        var pass = new ModelDirective.Pass()
        {
            Engine = projectEngine.Engine,
        };

        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);

        // Act
        pass.Execute(codeDocument, irDocument);

        // Assert
        var @class = FindClassNode(irDocument);
        Assert.NotNull(@class);
        var baseType = @class.BaseType;

        Assert.Equal("BaseType", baseType.BaseType.Content);
        Assert.Null(baseType.BaseType.Source);

        Assert.Equal("SomeType", baseType.ModelType.Content);
        Assert.Null(baseType.ModelType.Source);

        var @namespace = FindNamespaceNode(irDocument);
        var usingNode = Assert.IsType<UsingDirectiveIntermediateNode>(@namespace.Children[0]);
        Assert.Equal($"TModel = global::System.Object", usingNode.Content);
    }

    private static ClassDeclarationIntermediateNode FindClassNode(IntermediateNode node)
    {
        var visitor = new ClassNodeVisitor();
        visitor.Visit(node);
        return visitor.Node;
    }

    private static NamespaceDeclarationIntermediateNode FindNamespaceNode(IntermediateNode node)
    {
        var visitor = new NamespaceNodeVisitor();
        visitor.Visit(node);
        return visitor.Node;
    }

    private RazorProjectEngine CreateRuntimeEngine()
    {
        return CreateEngineCore(designTime: false);
    }

    private RazorProjectEngine CreateDesignTimeEngine()
    {
        return CreateEngineCore(designTime: true);
    }

    private RazorProjectEngine CreateEngineCore(bool designTime = false)
    {
        return CreateProjectEngine(b =>
        {
            // Notice we're not registering the ModelDirective.Pass here so we can run it on demand.
            b.AddDirective(ModelDirective.Directive);

            // There's some special interaction with the inherits directive
            InheritsDirective.Register(b);

            b.Features.Add(new RazorPageDocumentClassifierPass());
            b.Features.Add(new MvcViewDocumentClassifierPass());

            b.Features.Add(new DesignTimeOptionsFeature(designTime));
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

        // InheritsDirectivePass needs to run before ModelDirective.
        var pass = new InheritsDirectivePass()
        {
            Engine = engine
        };
        pass.Execute(codeDocument, codeDocument.GetDocumentIntermediateNode());

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

    private class NamespaceNodeVisitor : IntermediateNodeWalker
    {
        public NamespaceDeclarationIntermediateNode Node { get; set; }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationIntermediateNode node)
        {
            Node = node;
        }
    }

    private class DesignTimeOptionsFeature(bool designTime) : RazorEngineFeatureBase, IConfigureRazorParserOptionsFeature, IConfigureRazorCodeGenerationOptionsFeature
    {
        private readonly bool _designTime = designTime;

        public int Order { get; }

        public void Configure(RazorParserOptionsBuilder options)
        {
            options.SetDesignTime(_designTime);
        }

        public void Configure(RazorCodeGenerationOptionsBuilder options)
        {
            options.SetDesignTime(_designTime);
        }
    }
}
