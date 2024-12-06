// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

public class DocumentSnapshotTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private static readonly HostDocument s_componentHostDocument = TestProjectData.SomeProjectComponentFile1;
    private static readonly HostDocument s_componentCshtmlHostDocument = TestProjectData.SomeProjectCshtmlComponentFile5;
    private static readonly HostDocument s_legacyHostDocument = TestProjectData.SomeProjectFile1;
    private static readonly HostDocument s_nestedComponentHostDocument = TestProjectData.SomeProjectNestedComponentFile3;

    [Fact]
    public async Task GCCollect_OutputIsNoLongerCached()
    {
        // Arrange
        var document = CreateDocument(s_legacyHostDocument);
        await Task.Run(async () => { await document.GetGeneratedOutputAsync(DisposalToken); });

        // Act

        // Forces collection of the cached document output
        GC.Collect();

        // Assert
        Assert.False(document.TryGetGeneratedOutput(out _));
    }

    [Fact]
    public async Task RegeneratingWithReference_CachesOutput()
    {
        // Arrange
        var document = CreateDocument(s_legacyHostDocument);
        var output = await document.GetGeneratedOutputAsync(DisposalToken);

        // Mostly doing this to ensure "var output" doesn't get optimized out
        Assert.NotNull(output);

        // Act & Assert
        Assert.True(document.TryGetGeneratedOutput(out _));
    }

    [Fact]
    public async Task GetGeneratedOutputAsync_CshtmlComponent_ContainsComponentImports()
    {
        // This is a sanity test that we invoke component codegen for components. It's a little fragile but necessary.

        // Act
        var document = CreateDocument(s_componentCshtmlHostDocument);
        var codeDocument = await document.GetGeneratedOutputAsync(DisposalToken);

        // Assert
        Assert.Contains("using global::Microsoft.AspNetCore.Components", codeDocument.GetCSharpSourceText().ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetGeneratedOutputAsync_Component()
    {
        // Act
        var document = CreateDocument(s_componentHostDocument);
        var codeDocument = await document.GetGeneratedOutputAsync(DisposalToken);

        // Assert
        Assert.Contains("ComponentBase", codeDocument.GetCSharpSourceText().ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetGeneratedOutputAsync_NestedComponentDocument_SetsCorrectNamespaceAndClassName()
    {
        // Act
        var document = CreateDocument(s_nestedComponentHostDocument);
        var codeDocument = await document.GetGeneratedOutputAsync(DisposalToken);

        // Assert
        Assert.Contains("ComponentBase", codeDocument.GetCSharpSourceText().ToString(), StringComparison.Ordinal);
        Assert.Contains("namespace SomeProject.Nested", codeDocument.GetCSharpSourceText().ToString(), StringComparison.Ordinal);
        Assert.Contains("class File3", codeDocument.GetCSharpSourceText().ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetGeneratedOutputAsync_Legacy()
    {
        // This is a sanity test that we invoke legacy codegen for .cshtml files. It's a little fragile but necessary.

        // Act
        var document = CreateDocument(s_legacyHostDocument);
        var codeDocument = await document.GetGeneratedOutputAsync(DisposalToken);

        // Assert
        Assert.Contains("Template", codeDocument.GetCSharpSourceText().ToString(), StringComparison.Ordinal);
    }

    private static DocumentSnapshot CreateDocument(HostDocument hostDocument)
    {
        var textLoader = TestMocks.CreateTextLoader("<p>Hello World</p>");

        var state = ProjectState
            .Create(TestProjectData.SomeProject)
            .AddDocument(hostDocument, textLoader);

        var project = new ProjectSnapshot(state);
        var document = (DocumentSnapshot?)project.GetDocument(hostDocument.FilePath);
        Assert.NotNull(document);

        return document;
    }
}
