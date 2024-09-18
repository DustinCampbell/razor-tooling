// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

public class DocumentSnapshotTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private readonly DocumentSnapshot _componentDocument = CreateDocumentSnapshot(TestProjectData.SomeProjectComponentFile1);
    private readonly DocumentSnapshot _componentCshtmlDocument = CreateDocumentSnapshot(TestProjectData.SomeProjectCshtmlComponentFile5);
    private readonly DocumentSnapshot _legacyDocument = CreateDocumentSnapshot(TestProjectData.SomeProjectFile1);
    private readonly DocumentSnapshot _nestedComponentDocument = CreateDocumentSnapshot(TestProjectData.SomeProjectNestedComponentFile3);

    private static DocumentSnapshot CreateDocumentSnapshot(HostDocument hostDocument)
    {
        var projectState = ProjectState.Create(ProjectEngineFactories.DefaultProvider, TestProjectData.SomeProject, ProjectWorkspaceState.Default);
        var project = new ProjectSnapshot(projectState);

        return new DocumentSnapshot(project, DocumentState.Create(hostDocument,
            () => Task.FromResult(TextAndVersion.Create(SourceText.From("<p>Hello World</p>"), VersionStamp.Create()))));
    }

    [Fact]
    public async Task GCCollect_OutputIsNoLongerCached()
    {
        // Arrange
        await Task.Run(async () => { await _legacyDocument.GetGeneratedOutputAsync(); });

        // Act

        // Forces collection of the cached document output
        GC.Collect();

        // Assert
        Assert.False(_legacyDocument.TryGetGeneratedOutput(out _));
    }

    [Fact]
    public async Task RegeneratingWithReference_CachesOutput()
    {
        // Arrange
        var output = await _legacyDocument.GetGeneratedOutputAsync();

        // Mostly doing this to ensure "var output" doesn't get optimized out
        Assert.NotNull(output);

        // Act & Assert
        Assert.True(_legacyDocument.TryGetGeneratedOutput(out _));
    }

    // This is a sanity test that we invoke component codegen for components.It's a little fragile but
    // necessary.

    [Fact]
    public async Task GetGeneratedOutputAsync_CshtmlComponent_ContainsComponentImports()
    {
        // Act
        var codeDocument = await _componentCshtmlDocument.GetGeneratedOutputAsync();

        // Assert
        Assert.Contains("using global::Microsoft.AspNetCore.Components", codeDocument.GetCSharpSourceText().ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetGeneratedOutputAsync_Component()
    {
        // Act
        var codeDocument = await _componentDocument.GetGeneratedOutputAsync();

        // Assert
        Assert.Contains("ComponentBase", codeDocument.GetCSharpSourceText().ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetGeneratedOutputAsync_NestedComponentDocument_SetsCorrectNamespaceAndClassName()
    {
        // Act
        var codeDocument = await _nestedComponentDocument.GetGeneratedOutputAsync();

        // Assert
        Assert.Contains("ComponentBase", codeDocument.GetCSharpSourceText().ToString(), StringComparison.Ordinal);
        Assert.Contains("namespace SomeProject.Nested", codeDocument.GetCSharpSourceText().ToString(), StringComparison.Ordinal);
        Assert.Contains("class File3", codeDocument.GetCSharpSourceText().ToString(), StringComparison.Ordinal);
    }

    // This is a sanity test that we invoke legacy codegen for .cshtml files. It's a little fragile but
    // necessary.
    [Fact]
    public async Task GetGeneratedOutputAsync_Legacy()
    {
        // Act
        var codeDocument = await _legacyDocument.GetGeneratedOutputAsync();

        // Assert
        Assert.Contains("Template", codeDocument.GetCSharpSourceText().ToString(), StringComparison.Ordinal);
    }
}
