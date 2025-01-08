// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.NET.Sdk.Razor.SourceGenerators;
using Moq;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.CommonMetadata;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.IntermediateNodeAssert;

namespace Microsoft.AspNetCore.Razor.Language;

public class DefaultRazorIntermediateNodeLoweringPhaseIntegrationTest
{
    [Fact]
    public void Lower_SetsOptions_Defaults()
    {
        // Arrange
        var source = TestRazorSourceDocument.CreateEmpty();

        // Act
        var documentNode = Lower(source);

        // Assert
        Assert.NotNull(documentNode.Options);
        Assert.False(documentNode.Options.DesignTime);
        Assert.Equal(4, documentNode.Options.IndentSize);
        Assert.False(documentNode.Options.IndentWithTabs);
    }

    [Fact]
    public void Lower_SetsOptions_RunsConfigureCallbacks()
    {
        // Arrange
        var source = TestRazorSourceDocument.CreateEmpty();

        var callback = new Mock<IConfigureRazorCodeGenerationOptionsFeature>();
        callback
            .Setup(c => c.Configure(It.IsAny<RazorCodeGenerationOptionsBuilder>()))
            .Callback<RazorCodeGenerationOptionsBuilder>(o =>
            {
                o.IndentSize = 17;
                o.IndentWithTabs = true;
                o.SuppressChecksum = true;
            });

        // Act
        var documentNode = Lower(
            source,
            builder: b =>
            {
                b.Features.Add(callback.Object);
            },
            designTime: true);

        // Assert
        Assert.NotNull(documentNode.Options);
        Assert.True(documentNode.Options.DesignTime);
        Assert.Equal(17, documentNode.Options.IndentSize);
        Assert.True(documentNode.Options.IndentWithTabs);
        Assert.True(documentNode.Options.SuppressChecksum);
    }

    [Fact]
    public void Lower_HelloWorld()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("Hello, World!");

        // Act
        var documentNode = Lower(source);

        // Assert
        Children(documentNode,
            n => Html("Hello, World!", n));
    }

    [Fact]
    public void Lower_HtmlWithDataDashAttributes()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(@"
<html>
    <body>
        <span data-val=""@Hello"" />
    </body>
</html>");

        // Act
        var documentNode = Lower(source);

        // Assert
        Children(documentNode,
            n => Html(
@"
<html>
    <body>
        <span data-val=""", n),
            n => CSharpExpression("Hello", n),
            n => Html(@""" />
    </body>
</html>", n));
    }

    [Fact]
    public void Lower_HtmlWithConditionalAttributes()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(@"
<html>
    <body>
        <span val=""@Hello World"" />
    </body>
</html>");

        // Act
        var documentNode = Lower(source);

        // Assert
        Children(documentNode,
            n => Html(
@"
<html>
    <body>
        <span", n),

            n => ConditionalAttribute(
                prefix: " val=\"",
                name: "val",
                suffix: "\"",
                node: n,
                valueValidators: [
                    value => CSharpExpressionAttributeValue(string.Empty, "Hello", value),
                    value => LiteralAttributeValue(" ",  "World", value)
                ]),
            n => Html(@" />
    </body>
</html>", n));
    }

    [Fact]
    public void Lower_WithFunctions()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(@"@functions { public int Foo { get; set; }}");

        // Act
        var documentNode = Lower(source);

        // Assert
        Children(documentNode,
            n => Directive(
                "functions",
                n,
                c => Assert.IsType<CSharpCodeIntermediateNode>(c)));
    }

    [Fact]
    public void Lower_WithUsing()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(@"@using System");
        var expectedSourceLocation = new SourceSpan(source.FilePath, 1, 0, 1, 12);

        // Act
        var documentNode = Lower(source);

        // Assert
        Children(documentNode,
            n =>
            {
                Using("System", n);
                Assert.Equal(expectedSourceLocation, n.Source);
            });
    }

    [Fact]
    public void Lower_TagHelpers()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(@"@addTagHelper *, TestAssembly
<span val=""@Hello World""></span>");
        var tagHelpers = new[]
        {
            CreateTagHelperDescriptor(
                tagName: "span",
                typeName: "SpanTagHelper",
                assemblyName: "TestAssembly")
        };

        // Act
        var documentNode = Lower(source, tagHelpers: tagHelpers);

        // Assert
        Children(documentNode,
            n => Directive(
                SyntaxConstants.CSharp.AddTagHelperKeyword,
                n,
                v => DirectiveToken(DirectiveTokenKind.String, "*, TestAssembly", v)),
            n => TagHelper(
                "span",
                TagMode.StartTagAndEndTag,
                tagHelpers,
                n,
                c => Assert.IsType<TagHelperBodyIntermediateNode>(c),
                c => TagHelperHtmlAttribute(
                    "val",
                    AttributeStructure.DoubleQuotes,
                    c,
                    v => CSharpExpressionAttributeValue(string.Empty, "Hello", v),
                    v => LiteralAttributeValue(" ", "World", v))));
    }

    [Fact]
    public void Lower_TagHelpers_WithPrefix()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(@"@addTagHelper *, TestAssembly
@tagHelperPrefix cool:
<cool:span val=""@Hello World""></cool:span>");
        var tagHelpers = new[]
        {
                CreateTagHelperDescriptor(
                    tagName: "span",
                    typeName: "SpanTagHelper",
                    assemblyName: "TestAssembly")
            };

        // Act
        var documentNode = Lower(source, tagHelpers: tagHelpers);

        // Assert
        Children(documentNode,
            n => Directive(
                SyntaxConstants.CSharp.AddTagHelperKeyword,
                n,
                v => DirectiveToken(DirectiveTokenKind.String, "*, TestAssembly", v)),
            n => Directive(
                SyntaxConstants.CSharp.TagHelperPrefixKeyword,
                n,
                v => DirectiveToken(DirectiveTokenKind.String, "cool:", v)),
            n => TagHelper(
                "span",  // Note: this is span not cool:span
                TagMode.StartTagAndEndTag,
                tagHelpers,
                n,
                c => Assert.IsType<TagHelperBodyIntermediateNode>(c),
                c => TagHelperHtmlAttribute(
                    "val",
                    AttributeStructure.DoubleQuotes,
                    c,
                    v => CSharpExpressionAttributeValue(string.Empty, "Hello", v),
                    v => LiteralAttributeValue(" ", "World", v))));
    }

    [Fact]
    public void Lower_TagHelper_InSection()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(@"@addTagHelper *, TestAssembly
@section test {
<span val=""@Hello World""></span>
}");
        var tagHelpers = new[]
        {
            CreateTagHelperDescriptor(
                tagName: "span",
                typeName: "SpanTagHelper",
                assemblyName: "TestAssembly")
        };

        // Act
        var documentNode = Lower(source, tagHelpers: tagHelpers);

        // Assert
        Children(
            documentNode,
            n => Directive(
                SyntaxConstants.CSharp.AddTagHelperKeyword,
                n,
                v => DirectiveToken(DirectiveTokenKind.String, "*, TestAssembly", v)),
            n => Directive(
                "section",
                n,
                c1 => DirectiveToken(DirectiveTokenKind.Member, "test", c1),
                c1 => Html(Environment.NewLine, c1),
                c1 => TagHelper(
                    "span",
                    TagMode.StartTagAndEndTag,
                    tagHelpers,
                    c1,
                    c2 => Assert.IsType<TagHelperBodyIntermediateNode>(c2),
                    c2 => TagHelperHtmlAttribute(
                        "val",
                        AttributeStructure.DoubleQuotes,
                        c2,
                        v => CSharpExpressionAttributeValue(string.Empty, "Hello", v),
                        v => LiteralAttributeValue(" ", "World", v))),
                c1 => Html(Environment.NewLine, c1)));
    }

    [Fact]
    public void Lower_TagHelpersWithBoundAttribute()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(@"@addTagHelper *, TestAssembly
<input bound='foo' />");
        var tagHelpers = new[]
        {
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "InputTagHelper",
                    assemblyName: "TestAssembly",
                    attributes: new Action<BoundAttributeDescriptorBuilder>[]
                    {
                        builder => builder
                            .Name("bound")
                            .Metadata(PropertyName("FooProp"))
                            .TypeName("System.String"),
                            })
            };

        // Act
        var documentNode = Lower(source, tagHelpers: tagHelpers);

        // Assert
        Children(
            documentNode,
            n => Directive(
                SyntaxConstants.CSharp.AddTagHelperKeyword,
                n,
                v => DirectiveToken(DirectiveTokenKind.String, "*, TestAssembly", v)),
            n => TagHelper(
                "input",
                TagMode.SelfClosing,
                tagHelpers,
                n,
                c => Assert.IsType<TagHelperBodyIntermediateNode>(c),
                c => SetTagHelperProperty(
                    "bound",
                    "FooProp",
                    AttributeStructure.SingleQuotes,
                    c,
                    v => Html("foo", v))));
    }

    [Fact]
    public void Lower_WithImports_Using()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(@"@using System.Threading.Tasks
<p>Hi!</p>");
        var importSource1 = TestRazorSourceDocument.Create("@using System.Globalization");
        var importSource2 = TestRazorSourceDocument.Create("@using System.Text");

        // Act
        var documentNode = Lower(source, [importSource1, importSource2]);

        // Assert
        Children(
            documentNode,
            n => Using("System.Globalization", n),
            n => Using("System.Text", n),
            n => Using("System.Threading.Tasks", n),
            n => Html("<p>Hi!</p>", n));
    }

    [Fact]
    public void Lower_WithImports_AllowsIdenticalNamespacesInPrimaryDocument()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(@"@using System.Threading.Tasks
@using System.Threading.Tasks");
        var importSource = TestRazorSourceDocument.Create("@using System.Threading.Tasks");

        // Act
        var documentNode = Lower(source, [importSource]);

        // Assert
        Children(
            documentNode,
            n => Using("System.Threading.Tasks", n),
            n => Using("System.Threading.Tasks", n));
    }

    [Fact]
    public void Lower_WithMultipleImports_SingleLineFileScopedSinglyOccurring()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("<p>Hi!</p>");
        var importSource1 = TestRazorSourceDocument.Create("@test value1");
        var importSource2 = TestRazorSourceDocument.Create("@test value2");

        // Act
        var documentNode = Lower(source, [importSource1, importSource2], b =>
        {
            b.AddDirective(DirectiveDescriptor.CreateDirective(
                "test",
                DirectiveKind.SingleLine,
                builder =>
                {
                    builder.AddMemberToken();
                    builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
                }));
        });

        // Assert
        Children(
            documentNode,
            n => Directive("test", n, c => DirectiveToken(DirectiveTokenKind.Member, "value2", c)),
            n => Html("<p>Hi!</p>", n));
    }

    [Fact]
    public void Lower_WithImports_IgnoresBlockDirective()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("<p>Hi!</p>");
        var importSource = TestRazorSourceDocument.Create("@block token { }");

        // Act
        var documentNode = Lower(source, [importSource], b =>
        {
            b.AddDirective(DirectiveDescriptor.CreateDirective("block", DirectiveKind.RazorBlock, d => d.AddMemberToken()));
        });

        // Assert
        Children(
            documentNode,
            n => Html("<p>Hi!</p>", n));
    }

    private static DocumentIntermediateNode Lower(
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> importSources = default,
        Action<RazorProjectEngineBuilder> builder = null,
        IEnumerable<TagHelperDescriptor> tagHelpers = null,
        bool designTime = false)
    {
        tagHelpers ??= [];

        Action<RazorProjectEngineBuilder> configureEngine = b =>
        {
            builder?.Invoke(b);

            SectionDirective.Register(b);
            b.AddTagHelpers(tagHelpers);

            b.Features.Add(new DesignTimeOptionsFeature(designTime));
            b.Features.Add(new ConfigureRazorParserOptions(useRoslynTokenizer: true, CSharpParseOptions.Default));
        };

        var projectEngine = RazorProjectEngine.Create(configureEngine);

        var codeDocument = projectEngine.CreateCodeDocument(source, importSources, FileKinds.Legacy);

        foreach (var phase in projectEngine.Phases)
        {
            phase.Execute(codeDocument);

            if (phase is IRazorIntermediateNodeLoweringPhase)
            {
                break;
            }
        }

        var documentNode = codeDocument.GetDocumentIntermediateNode();
        Assert.NotNull(documentNode);

        return documentNode;
    }

    private static TagHelperDescriptor CreateTagHelperDescriptor(
        string tagName,
        string typeName,
        string assemblyName,
        IEnumerable<Action<BoundAttributeDescriptorBuilder>> attributes = null)
    {
        var builder = TagHelperDescriptorBuilder.Create(typeName, assemblyName);
        builder.Metadata(TypeName(typeName));

        if (attributes != null)
        {
            foreach (var attributeBuilder in attributes)
            {
                builder.BoundAttributeDescriptor(attributeBuilder);
            }
        }

        builder.TagMatchingRuleDescriptor(ruleBuilder => ruleBuilder.RequireTagName(tagName));

        var descriptor = builder.Build();

        return descriptor;
    }

    private class DesignTimeOptionsFeature(bool designTime) : RazorEngineFeatureBase, IConfigureRazorParserOptionsFeature, IConfigureRazorCodeGenerationOptionsFeature
    {
        private readonly bool _designTime = designTime;

        public int Order { get; }

        public void Configure(RazorParserOptionsBuilder options)
        {
            options.SetDesignTime(_designTime);
            options.UseRoslynTokenizer = true;
        }

        public void Configure(RazorCodeGenerationOptionsBuilder options)
        {
            options.SetDesignTime(_designTime);
        }
    }
}
