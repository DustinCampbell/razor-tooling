// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Workspaces;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

public class ProjectSnapshotTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private static readonly HostProject s_hostProject = TestProjectData.SomeProject with { Configuration = FallbackRazorConfiguration.MVC_2_0 };
    private static readonly ProjectWorkspaceState s_projectWorkspaceState = ProjectWorkspaceState.Create([TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly").Build()]);

    private static readonly HostDocument[] s_documents =
    [
        TestProjectData.SomeProjectFile1,
        TestProjectData.SomeProjectFile2,

        // linked file
        TestProjectData.AnotherProjectNestedFile3
    ];

    [Fact]
    public void ProjectSnapshot_CachesDocumentSnapshots()
    {
        // Arrange
        var state = ProjectState
            .Create(ProjectEngineFactories.DefaultProvider, TestLanguageServerFeatureOptions.Instance, s_hostProject, s_projectWorkspaceState)
            .AddDocument(s_documents[0], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance)
            .AddDocument(s_documents[2], EmptyTextLoader.Instance);

        var project = new ProjectSnapshot(state);

        // Act
        var documents = project.DocumentFilePaths
            .Select(filePath => (filePath, document: project.GetDocument(filePath)));

        // Assert
        Assert.Collection(
            documents,
            t => Assert.Same(t.document, project.GetDocument(t.filePath)),
            t => Assert.Same(t.document, project.GetDocument(t.filePath)),
            t => Assert.Same(t.document, project.GetDocument(t.filePath)));
    }

    [Fact]
    public void GetRelatedDocuments_NonImportDocument_ReturnsEmpty()
    {
        // Arrange
        var state = ProjectState
            .Create(ProjectEngineFactories.DefaultProvider, TestLanguageServerFeatureOptions.Instance, s_hostProject, s_projectWorkspaceState)
            .AddDocument(s_documents[0], EmptyTextLoader.Instance);

        var project = new ProjectSnapshot(state);

        var document = project.GetDocument(s_documents[0].FilePath);
        Assert.NotNull(document);

        // Act
        var documents = project.GetRelatedDocuments(document);

        // Assert
        Assert.Empty(documents);
    }

    [Fact]
    public void GetRelatedDocuments_ImportDocument_ReturnsRelated()
    {
        // Arrange
        var state = ProjectState
            .Create(ProjectEngineFactories.DefaultProvider, TestLanguageServerFeatureOptions.Instance, s_hostProject, s_projectWorkspaceState)
            .AddDocument(s_documents[0], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance)
            .AddDocument(TestProjectData.SomeProjectImportFile, EmptyTextLoader.Instance);

        var project = new ProjectSnapshot(state);

        var document = project.GetDocument(TestProjectData.SomeProjectImportFile.FilePath);
        Assert.NotNull(document);

        // Act
        var documents = project.GetRelatedDocuments(document);

        // Assert
        Assert.Collection(
            documents.OrderBy(static d => d.FilePath),
            d => Assert.Equal(s_documents[0].FilePath, d.FilePath),
            d => Assert.Equal(s_documents[1].FilePath, d.FilePath));
    }
}
