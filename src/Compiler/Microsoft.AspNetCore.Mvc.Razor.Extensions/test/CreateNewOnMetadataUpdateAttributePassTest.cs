// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public class CreateNewOnMetadataUpdateAttributePassTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Version_6_0;

    [Fact]
    public void Execute_AddsAttributes()
    {
        // Arrange
        var projectEngine = CreateProjectEngine(b =>
        {
            PageDirective.Register(b);
        });

        var properties = RazorSourceDocumentProperties.Create(filePath: "ignored", relativePath: "Test.cshtml");
        var source = RazorSourceDocument.Create("Hello world", properties);
        var codeDocument = projectEngine.CreateCodeDocument(source, FileKinds.Legacy);

        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);
        var pass = new CreateNewOnMetadataUpdateAttributePass
        {
            Engine = projectEngine.Engine
        };
        var documentClassifier = new MvcViewDocumentClassifierPass { Engine = projectEngine.Engine };

        // Act
        documentClassifier.Execute(codeDocument, irDocument);
        pass.Execute(codeDocument, irDocument);
        var visitor = new Visitor();
        visitor.Visit(irDocument);

        // Assert
        Assert.Collection(
            visitor.ExtensionNodes,
            node =>
            {
                var attributeNode = Assert.IsType<RazorCompiledItemMetadataAttributeIntermediateNode>(node);
                Assert.Equal("Identifier", attributeNode.Key);
                Assert.Equal("/Test.cshtml", attributeNode.Value);
            },
            node =>
            {
                Assert.IsType<CreateNewOnMetadataUpdateAttributePass.CreateNewOnMetadataUpdateAttributeIntermediateNode>(node);
            });
    }

    [Fact]
    public void Execute_NoOpsForBlazorComponents()
    {
        // Arrange
        var projectEngine = CreateProjectEngine(b =>
        {
            PageDirective.Register(b);
        });

        var properties = RazorSourceDocumentProperties.Create(filePath: "ignored", relativePath: "Test.razor");
        var source = RazorSourceDocument.Create("Hello world", properties);
        var codeDocument = projectEngine.CreateCodeDocument(source, FileKinds.Component);

        var irDocument = CreateIRDocument(projectEngine.Engine, codeDocument);
        var pass = new CreateNewOnMetadataUpdateAttributePass
        {
            Engine = projectEngine.Engine
        };
        var documentClassifier = new DefaultDocumentClassifierPass { Engine = projectEngine.Engine };

        // Act
        documentClassifier.Execute(codeDocument, irDocument);
        pass.Execute(codeDocument, irDocument);
        var visitor = new Visitor();
        visitor.Visit(irDocument);

        // Assert
        Assert.Empty(visitor.ExtensionNodes);
    }

    private static DocumentIntermediateNode CreateIRDocument(RazorEngine engine, RazorCodeDocument codeDocument)
    {
        foreach (var phase in engine.Phases)
        {
            phase.Execute(codeDocument);

            if (phase is IRazorIntermediateNodeLoweringPhase)
            {
                break;
            }
        }

        return codeDocument.GetDocumentIntermediateNode();
    }

    private class Visitor : IntermediateNodeWalker
    {
        public List<ExtensionIntermediateNode> ExtensionNodes { get; } = new();

        public override void VisitExtension(ExtensionIntermediateNode node)
        {
            ExtensionNodes.Add(node);
        }
    }
}
