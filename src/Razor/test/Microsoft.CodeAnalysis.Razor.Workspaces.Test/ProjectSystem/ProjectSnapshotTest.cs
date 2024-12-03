// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Workspaces;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

public class ProjectSnapshotTest : WorkspaceTestBase
{
    private readonly HostDocument[] _documents;
    private readonly HostProject _hostProject;
    private readonly ProjectWorkspaceState _projectWorkspaceState;

    public ProjectSnapshotTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        _hostProject = TestProjectData.SomeProject with { Configuration = FallbackRazorConfiguration.MVC_2_0 };
        _projectWorkspaceState = ProjectWorkspaceState.Create([TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly").Build()]);

        _documents =
        [
            TestProjectData.SomeProjectFile1,
            TestProjectData.SomeProjectFile2,

            // linked file
            TestProjectData.AnotherProjectNestedFile3,
        ];
    }

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
        builder.SetImportFeature(new TestImportProjectFeature());
    }

    [Fact]
    public void ProjectSnapshot_CachesDocumentSnapshots()
    {
        // Arrange
        var solutionState = SolutionState
            .Create(ProjectEngineFactoryProvider, LanguageServerFeatureOptions)
            .AddProject(_hostProject)
            .UpdateProjectWorkspaceState(_hostProject.Key, _projectWorkspaceState)
            .AddDocument(_hostProject.Key, _documents[0], DocumentState.EmptyLoader)
            .AddDocument(_hostProject.Key, _documents[1], DocumentState.EmptyLoader)
            .AddDocument(_hostProject.Key, _documents[2], DocumentState.EmptyLoader);

        var solution = new SolutionSnapshot(solutionState);
        var project = solution.GetRequiredProject(_hostProject.Key);

        // Act
        var documents = project.DocumentFilePaths.ToDictionary(f => f, f => project.GetDocument(f));

        // Assert
        Assert.Collection(
            documents,
            d => Assert.Same(d.Value, project.GetDocument(d.Key)),
            d => Assert.Same(d.Value, project.GetDocument(d.Key)),
            d => Assert.Same(d.Value, project.GetDocument(d.Key)));
    }

    [Fact]
    public void GetRelatedDocuments_NonImportDocument_ReturnsEmpty()
    {
        // Arrange
        var solutionState = SolutionState
            .Create(ProjectEngineFactoryProvider, LanguageServerFeatureOptions)
            .AddProject(_hostProject)
            .UpdateProjectWorkspaceState(_hostProject.Key, _projectWorkspaceState)
            .AddDocument(_hostProject.Key, _documents[0], DocumentState.EmptyLoader);

        var solution = new SolutionSnapshot(solutionState);
        var project = solution.GetRequiredProject(_hostProject.Key);
        var document = project.GetDocument(_documents[0].FilePath).AssumeNotNull();

        // Act
        var documents = project.GetRelatedDocuments(document);

        // Assert
        Assert.Empty(documents);
    }

    [Fact]
    public void GetRelatedDocuments_ImportDocument_ReturnsRelated()
    {
        // Arrange
        var solutionState = SolutionState.Create(ProjectEngineFactoryProvider, LanguageServerFeatureOptions)
            .AddProject(_hostProject)
            .UpdateProjectWorkspaceState(_hostProject.Key, _projectWorkspaceState)
            .AddDocument(_hostProject.Key, _documents[0], DocumentState.EmptyLoader)
            .AddDocument(_hostProject.Key, _documents[1], DocumentState.EmptyLoader)
            .AddDocument(_hostProject.Key, TestProjectData.SomeProjectImportFile, DocumentState.EmptyLoader);

        var solution = new SolutionSnapshot(solutionState);
        var project = solution.GetRequiredProject(_hostProject.Key);
        var document = project.GetDocument(TestProjectData.SomeProjectImportFile.FilePath).AssumeNotNull();

        // Act
        var documents = project.GetRelatedDocuments(document);

        // Assert
        Assert.Collection(
            documents.OrderBy(d => d.FilePath),
            d => Assert.Equal(_documents[0].FilePath, d.FilePath),
            d => Assert.Equal(_documents[1].FilePath, d.FilePath));
    }
}
