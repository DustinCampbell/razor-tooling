// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Workspaces;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

public class DefaultProjectSnapshotTest(ITestOutputHelper testOutput) : WorkspaceTestBase(testOutput)
{
    private static readonly ImmutableArray<HostDocument> s_documents = [
        TestProjectData.SomeProjectFile1,
        TestProjectData.SomeProjectFile2,

        // linked file
        TestProjectData.AnotherProjectNestedFile3,
    ];

    private static readonly HostProject s_hostProject = TestProjectData.SomeProject with { Configuration = FallbackRazorConfiguration.MVC_2_0 };
    private static readonly ProjectWorkspaceState s_projectWorkspaceState = ProjectWorkspaceState.Create([TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly").Build()]);

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
        builder.SetImportFeature(new TestImportProjectFeature());
    }

    [Fact]
    public void ProjectSnapshot_CachesDocumentSnapshots()
    {
        // Arrange
        var state = ProjectState.Create(ProjectEngineFactoryProvider, s_hostProject, s_projectWorkspaceState)
            .WithAddedHostDocument(s_documents[0], DocumentState.EmptyLoader)
            .WithAddedHostDocument(s_documents[1], DocumentState.EmptyLoader)
            .WithAddedHostDocument(s_documents[2], DocumentState.EmptyLoader);
        var snapshot = new ProjectSnapshot(state);

        // Act
        var documents = snapshot.DocumentFilePaths.ToDictionary(f => f, f => snapshot.GetDocument(f));

        // Assert
        Assert.Collection(
            documents,
            d => Assert.Same(d.Value, snapshot.GetDocument(d.Key)),
            d => Assert.Same(d.Value, snapshot.GetDocument(d.Key)),
            d => Assert.Same(d.Value, snapshot.GetDocument(d.Key)));
    }

    [Fact]
    public void GetRelatedDocuments_NonImportDocument_ReturnsEmpty()
    {
        // Arrange
        var state = ProjectState.Create(ProjectEngineFactoryProvider, s_hostProject, s_projectWorkspaceState)
            .WithAddedHostDocument(s_documents[0], DocumentState.EmptyLoader);
        var snapshot = new ProjectSnapshot(state);

        var document = snapshot.GetDocument(s_documents[0].FilePath);
        Assert.NotNull(document);

        // Act
        var documents = snapshot.GetRelatedDocuments(document);

        // Assert
        Assert.Empty(documents);
    }

    [Fact]
    public void GetRelatedDocuments_ImportDocument_ReturnsRelated()
    {
        // Arrange
        var state = ProjectState.Create(ProjectEngineFactoryProvider, s_hostProject, s_projectWorkspaceState)
            .WithAddedHostDocument(s_documents[0], DocumentState.EmptyLoader)
            .WithAddedHostDocument(s_documents[1], DocumentState.EmptyLoader)
            .WithAddedHostDocument(TestProjectData.SomeProjectImportFile, DocumentState.EmptyLoader);
        var snapshot = new ProjectSnapshot(state);

        var document = snapshot.GetDocument(TestProjectData.SomeProjectImportFile.FilePath);
        Assert.NotNull(document);

        // Act
        var documents = snapshot.GetRelatedDocuments(document);

        // Assert
        Assert.Collection(
            documents.OrderBy(d => d.FilePath),
            d => Assert.Equal(s_documents[0].FilePath, d.FilePath),
            d => Assert.Equal(s_documents[1].FilePath, d.FilePath));
    }
}
