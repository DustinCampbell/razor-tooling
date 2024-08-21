// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Microsoft.AspNetCore.Razor.LanguageServer.Hosting;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.LanguageServer;
using Microsoft.AspNetCore.Razor.Test.Common.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common.Workspaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting;

public abstract class FormattingTestBase : RazorToolingIntegrationTestBase
{
    protected FormattingTestBase(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        ITestOnlyLoggerExtensions.TestOnlyLoggingEnabled = true;
    }

    private protected async Task RunFormattingTestAsync(
        string input,
        string expected,
        int tabSize = 4,
        bool insertSpaces = true,
        string? fileKind = null,
        ImmutableArray<TagHelperDescriptor> tagHelpers = default,
        bool allowDiagnostics = false,
        RazorLSPOptions? razorLSPOptions = null,
        bool inGlobalNamespace = false)
    {
        // Run with and without forceRuntimeCodeGeneration
        await RunFormattingTestAsync(input, expected, tabSize, insertSpaces, fileKind, tagHelpers, allowDiagnostics, razorLSPOptions, inGlobalNamespace, forceRuntimeCodeGeneration: true);
        await RunFormattingTestAsync(input, expected, tabSize, insertSpaces, fileKind, tagHelpers, allowDiagnostics, razorLSPOptions, inGlobalNamespace, forceRuntimeCodeGeneration: false);
    }

    private async Task RunFormattingTestAsync(string input, string expected, int tabSize, bool insertSpaces, string? fileKind, ImmutableArray<TagHelperDescriptor> tagHelpers, bool allowDiagnostics, RazorLSPOptions? razorLSPOptions, bool inGlobalNamespace, bool forceRuntimeCodeGeneration)
    {
        // Arrange
        fileKind ??= FileKinds.Component;
        tagHelpers = tagHelpers.NullToEmpty();

        TestFileMarkupParser.GetSpans(input, out input, out ImmutableArray<TextSpan> spans);

        var source = SourceText.From(input);
        var range = spans.IsEmpty
            ? null
            : source.GetRange(spans.Single());

        var path = "file:///path/to/Document." + fileKind;
        var uri = new Uri(path);
        var (codeDocument, documentSnapshot) = CreateCodeDocumentAndSnapshot(
            source, uri.AbsolutePath, fileKind, tagHelpers, allowDiagnostics, inGlobalNamespace, forceRuntimeCodeGeneration);
        var options = new FormattingOptions()
        {
            TabSize = tabSize,
            InsertSpaces = insertSpaces,
        };

        var formattingService = await TestRazorFormattingService.CreateWithFullSupportAsync(LoggerFactory, codeDocument, documentSnapshot, razorLSPOptions);
        var documentContext = new VersionedDocumentContext(uri, documentSnapshot, projectContext: null, version: 1);

        // Act
        var edits = await formattingService.FormatAsync(documentContext, range, options, DisposalToken);

        // Assert
        var edited = ApplyEdits(source, edits);
        var actual = edited.ToString();

        AssertEx.EqualOrDiff(expected, actual);

        if (input.Equals(expected))
        {
            Assert.Empty(edits);
        }
    }

    private protected async Task RunOnTypeFormattingTestAsync(
        string input,
        string expected,
        char triggerCharacter,
        int tabSize = 4,
        bool insertSpaces = true,
        string? fileKind = null,
        int? expectedChangedLines = null,
        RazorLSPOptions? razorLSPOptions = null,
        bool inGlobalNamespace = false)
    {
        // Arrange
        fileKind ??= FileKinds.Component;

        TestFileMarkupParser.GetPosition(input, out input, out var positionAfterTrigger);

        var razorSourceText = SourceText.From(input);
        var path = "file:///path/to/Document.razor";
        var uri = new Uri(path);
        var (codeDocument, documentSnapshot) = CreateCodeDocumentAndSnapshot(razorSourceText, uri.AbsolutePath, fileKind, inGlobalNamespace);

        var filePathService = new LSPFilePathService(TestLanguageServerFeatureOptions.Instance);
        var mappingService = new LspDocumentMappingService(
            filePathService, new TestDocumentContextFactory(), LoggerFactory);
        var languageKind = mappingService.GetLanguageKind(codeDocument, positionAfterTrigger, rightAssociative: false);

        var formattingService = await TestRazorFormattingService.CreateWithFullSupportAsync(
            LoggerFactory, codeDocument, documentSnapshot, razorLSPOptions);
        var options = new FormattingOptions()
        {
            TabSize = tabSize,
            InsertSpaces = insertSpaces,
        };
        var documentContext = new VersionedDocumentContext(uri, documentSnapshot, projectContext: null, version: 1);

        // Act
        var edits = await formattingService.FormatOnTypeAsync(
            documentContext, languageKind, formattedEdits: [], options, hostDocumentIndex: positionAfterTrigger, triggerCharacter, DisposalToken);

        // Assert
        var edited = ApplyEdits(razorSourceText, edits);
        var actual = edited.ToString();

        AssertEx.EqualOrDiff(expected, actual);

        if (input.Equals(expected))
        {
            Assert.Empty(edits);
        }

        if (expectedChangedLines is not null)
        {
            var firstLine = edits.Min(e => e.Range.Start.Line);
            var lastLine = edits.Max(e => e.Range.End.Line);
            var delta = lastLine - firstLine + edits.Count(e => e.NewText.Contains(Environment.NewLine));
            Assert.Equal(expectedChangedLines.Value, delta + 1);
        }
    }

    protected async Task RunCodeActionFormattingTestAsync(
        string input,
        TextEdit[] codeActionEdits,
        string expected,
        int tabSize = 4,
        bool insertSpaces = true,
        string? fileKind = null,
        bool inGlobalNamespace = false)
    {
        if (codeActionEdits is null)
        {
            throw new NotImplementedException("Code action formatting must provide edits.");
        }

        // Arrange
        fileKind ??= FileKinds.Component;

        TestFileMarkupParser.GetPosition(input, out input, out var positionAfterTrigger);

        var razorSourceText = SourceText.From(input);
        var path = "file:///path/to/Document.razor";
        var uri = new Uri(path);
        var (codeDocument, documentSnapshot) = CreateCodeDocumentAndSnapshot(razorSourceText, uri.AbsolutePath, fileKind, inGlobalNamespace);

        var filePathService = new LSPFilePathService(TestLanguageServerFeatureOptions.Instance);
        var mappingService = new LspDocumentMappingService(filePathService, new TestDocumentContextFactory(), LoggerFactory);
        var languageKind = mappingService.GetLanguageKind(codeDocument, positionAfterTrigger, rightAssociative: false);
        if (languageKind == RazorLanguageKind.Html)
        {
            throw new NotImplementedException("Code action formatting is not yet supported for HTML in Razor.");
        }

        if (!mappingService.TryMapToGeneratedDocumentPosition(codeDocument.GetCSharpDocument(), positionAfterTrigger, out _, out var _))
        {
            throw new InvalidOperationException("Could not map from Razor document to generated document");
        }

        var formattingService = await TestRazorFormattingService.CreateWithFullSupportAsync(LoggerFactory, codeDocument);
        var options = new FormattingOptions()
        {
            TabSize = tabSize,
            InsertSpaces = insertSpaces,
        };
        var documentContext = new VersionedDocumentContext(uri, documentSnapshot, projectContext: null, version: 1);

        // Act
        var edits = await formattingService.FormatCodeActionAsync(documentContext, languageKind, codeActionEdits, options, DisposalToken);

        // Assert
        var edited = ApplyEdits(razorSourceText, edits);
        var actual = edited.ToString();

        AssertEx.EqualOrDiff(expected, actual);
    }

    protected static TextEdit Edit(int startLine, int startChar, int endLine, int endChar, string newText)
        => VsLspFactory.CreateTextEdit(startLine, startChar, endLine, endChar, newText);

    private static SourceText ApplyEdits(SourceText source, TextEdit[] edits)
    {
        var changes = edits.Select(source.GetTextChange);
        return source.WithChanges(changes);
    }

    private static (RazorCodeDocument, IDocumentSnapshot) CreateCodeDocumentAndSnapshot(SourceText text, string path, string? fileKind, bool inGlobalNamespace)
        => CreateCodeDocumentAndSnapshot(text, path, fileKind, tagHelpers: default, allowDiagnostics: false, inGlobalNamespace, forceRuntimeCodeGeneration: false);

    private static (RazorCodeDocument, IDocumentSnapshot) CreateCodeDocumentAndSnapshot(
        SourceText text,
        string path, string? fileKind,
        ImmutableArray<TagHelperDescriptor> tagHelpers,
        bool allowDiagnostics, bool inGlobalNamespace, bool forceRuntimeCodeGeneration)
    {
        fileKind ??= FileKinds.Component;
        tagHelpers = tagHelpers.NullToEmpty();

        if (fileKind == FileKinds.Component)
        {
            tagHelpers = tagHelpers.AddRange(RazorTestResources.BlazorServerAppTagHelpers);
        }

        var sourceDocument = RazorSourceDocument.Create(text, RazorSourceDocumentProperties.Create(
            filePath: path,
            relativePath: inGlobalNamespace ? Path.GetFileName(path) : path));

        const string DefaultImports = """
                @using BlazorApp1
                @using BlazorApp1.Pages
                @using BlazorApp1.Shared
                @using Microsoft.AspNetCore.Components
                @using Microsoft.AspNetCore.Components.Authorization
                @using Microsoft.AspNetCore.Components.Routing
                @using Microsoft.AspNetCore.Components.Web
                """;

        var importsPath = new Uri("file:///path/to/_Imports.razor").AbsolutePath;
        var importsSourceDocument = RazorSourceDocument.Create(DefaultImports, RazorSourceDocumentProperties.Create(filePath: importsPath, relativePath: importsPath));

        var projectFileSystem = new TestRazorProjectFileSystem([
            new TestRazorProjectItem(path, fileKind: fileKind),
            new TestRazorProjectItem(importsPath, fileKind: FileKinds.ComponentImport)]);

        var projectEngine = RazorProjectEngine.Create(
            new RazorConfiguration(RazorLanguageVersion.Latest, "TestConfiguration", Extensions: [], new LanguageServerFlags(forceRuntimeCodeGeneration)),
            projectFileSystem,
            builder =>
            {
                builder.SetRootNamespace(inGlobalNamespace ? string.Empty : "Test");
                builder.Features.Add(new DefaultTypeNameFeature());
                RazorExtensions.Register(builder);
            });

        ImmutableArray<RazorSourceDocument> importsSources = [importsSourceDocument];
        var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, fileKind, importsSources, tagHelpers);

        if (!allowDiagnostics)
        {
            Assert.False(codeDocument.GetCSharpDocument().Diagnostics.Any(), "Error creating document:" + Environment.NewLine + string.Join(Environment.NewLine, codeDocument.GetCSharpDocument().Diagnostics));
        }

        var documentSnapshot = CreateDocumentSnapshot(path, tagHelpers, fileKind, importsSources, projectEngine, codeDocument, inGlobalNamespace);

        return (codeDocument, documentSnapshot);
    }

    internal static IDocumentSnapshot CreateDocumentSnapshot(
        string path, ImmutableArray<TagHelperDescriptor> tagHelpers,
        string? fileKind,
        ImmutableArray<RazorSourceDocument> importSources,
        RazorProjectEngine projectEngine,
        RazorCodeDocument codeDocument,
        bool inGlobalNamespace = false)
    {
        return TestMocks.CreateDocumentSnapshot(b =>
        {
            b.SetupProject(b =>
            {
                b.SetupKey(TestProjectKey.Create("/obj"));
                b.SetupConfiguration(projectEngine.Configuration);
                b.SetupProjectEngine(projectEngine);
                b.SetupTagHelpers(tagHelpers);
            });

            b.SetupFileKind(fileKind);
            b.SetupFilePath(path);
            b.SetupTargetPath(path);
            b.SetupGeneratedOutput(codeDocument);

            b.SetupWithText(text =>
            {
                var relativePath = inGlobalNamespace ? Path.GetFileName(path) : path;
                var sourceDocument = RazorSourceDocument.Create(text, RazorSourceDocumentProperties.Create(filePath: path, relativePath));
                var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, fileKind, importSources, tagHelpers);
                return CreateDocumentSnapshot(path, tagHelpers, fileKind, importSources, projectEngine, codeDocument, inGlobalNamespace);
            });
        });
    }
}
