// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.LanguageServer;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.NET.Sdk.Razor.SourceGenerators;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting;

public class FormattingContentValidationPassTest(ITestOutputHelper testOutput) : LanguageServerTestBase(testOutput)
{
    [Fact]
    public async Task Execute_NonDestructiveEdit_Allowed()
    {
        // Arrange
        TestCode source = """
            @code {
            [||]public class Foo { }
            }
            """;
        var context = CreateFormattingContext(source);
        var edits = ImmutableArray.Create(new TextChange(source.Span, "    "));
        var input = edits;
        var pass = GetPass();

        // Act
        var result = await pass.ExecuteAsync(context, edits, DisposalToken);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public async Task Execute_DestructiveEdit_Rejected()
    {
        // Arrange
        TestCode source = """
            @code {
            [|public class Foo { }
            |]}
            """;
        var context = CreateFormattingContext(source);
        var edits = ImmutableArray.Create(new TextChange(source.Span, "    "));
        var input = edits;
        var pass = GetPass();

        // Act
        var result = await pass.ExecuteAsync(context, input, DisposalToken);

        // Assert
        Assert.Empty(result);
    }

    private FormattingContentValidationPass GetPass()
    {
        var pass = new FormattingContentValidationPass(LoggerFactory)
        {
            DebugAssertsEnabled = false
        };

        return pass;
    }

    private static FormattingContext CreateFormattingContext(TestCode input, int tabSize = 4, bool insertSpaces = true, string? fileKind = null)
    {
        var source = SourceText.From(input.Text);
        var path = "file:///path/to/document.razor";
        var uri = new Uri(path);
        var (document, codeDocument) = CreateDocument(source, uri.AbsolutePath, fileKind: fileKind);
        var options = new RazorFormattingOptions()
        {
            TabSize = tabSize,
            InsertSpaces = insertSpaces,
        };

        return FormattingContext.Create(document, codeDocument, options);
    }

    private static (IRazorDocument, RazorCodeDocument) CreateDocument(SourceText text, string path, ImmutableArray<TagHelperDescriptor> tagHelpers = default, string? fileKind = null)
    {
        fileKind ??= FileKinds.Component;
        tagHelpers = tagHelpers.NullToEmpty();
        var sourceDocument = RazorSourceDocument.Create(text, RazorSourceDocumentProperties.Create(path, path));
        var projectEngine = RazorProjectEngine.Create(builder =>
        {
            builder.SetRootNamespace("Test");
            builder.Features.Add(new ConfigureRazorParserOptions(useRoslynTokenizer: true, CSharpParseOptions.Default));
        });
        var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, fileKind, importSources: default, tagHelpers);

        var documentMock = new StrictMock<IRazorDocument>();
        documentMock
            .Setup(d => d.GetGeneratedOutputAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(codeDocument);
        documentMock
            .Setup(d => d.TargetPath)
            .Returns(path);
        documentMock
            .Setup(d => d.Project.GetTagHelpersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tagHelpers);
        documentMock
            .Setup(d => d.FileKind)
            .Returns(fileKind);

        return (documentMock.Object, codeDocument);
    }
}
