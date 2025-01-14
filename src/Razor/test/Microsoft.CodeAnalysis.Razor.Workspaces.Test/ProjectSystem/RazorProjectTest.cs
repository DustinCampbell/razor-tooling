// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Workspaces;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

public class RazorProjectTest(ITestOutputHelper testOutput) : WorkspaceTestBase(testOutput)
{
    private static readonly HostProject s_hostProject = TestProjectData.SomeProject with { Configuration = FallbackRazorConfiguration.MVC_2_0 };
    private static readonly ProjectWorkspaceState s_projectWorkspaceState = ProjectWorkspaceState.Create([TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly").Build()]);

    private static readonly HostDocument[] s_documents =
    [
        TestProjectData.SomeProjectFile1,
        TestProjectData.SomeProjectFile2,

        // linked file
        TestProjectData.AnotherProjectNestedFile3,
    ];

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
        builder.SetImportFeature(new TestImportProjectFeature());
    }

    [Fact]
    public void ProjectSnapshot_CachesDocumentSnapshots()
    {
        // Arrange
        var project = RazorProject
            .Create(s_hostProject, CompilerOptions, ProjectEngineFactoryProvider)
            .WithProjectWorkspaceState(s_projectWorkspaceState)
            .AddEmptyDocument(s_documents[0])
            .AddEmptyDocument(s_documents[1])
            .AddEmptyDocument(s_documents[2]);

        // Act
        var documents = project.DocumentFilePaths.ToDictionary(f => f, project.GetRequiredDocument);

        // Assert
        Assert.Collection(
            documents,
            d => Assert.Same(d.Value, project.GetRequiredDocument(d.Key)),
            d => Assert.Same(d.Value, project.GetRequiredDocument(d.Key)),
            d => Assert.Same(d.Value, project.GetRequiredDocument(d.Key)));
    }

    [Fact]
    public void GetRelatedDocuments_NonImportDocument_ReturnsEmpty()
    {
        // Arrange
        var project = RazorProject
            .Create(s_hostProject, CompilerOptions, ProjectEngineFactoryProvider)
            .WithProjectWorkspaceState(s_projectWorkspaceState)
            .AddEmptyDocument(s_documents[0]);

        var document = project.GetRequiredDocument(s_documents[0].FilePath);

        // Act
        var documents = project.GetRelatedDocuments(document);

        // Assert
        Assert.Empty(documents);
    }

    [Fact]
    public void GetRelatedDocuments_ImportDocument_ReturnsRelated()
    {
        // Arrange
        var project = RazorProject
            .Create(s_hostProject, CompilerOptions, ProjectEngineFactoryProvider)
            .WithProjectWorkspaceState(s_projectWorkspaceState)
            .AddEmptyDocument(s_documents[0])
            .AddEmptyDocument(s_documents[1])
            .AddEmptyDocument(TestProjectData.SomeProjectImportFile);

        var document = project.GetRequiredDocument(TestProjectData.SomeProjectImportFile.FilePath);

        // Act
        var documents = project.GetRelatedDocuments(document);

        // Assert
        Assert.Collection(
            documents.OrderBy(d => d.FilePath),
            d => Assert.Equal(s_documents[0].FilePath, d.FilePath),
            d => Assert.Equal(s_documents[1].FilePath, d.FilePath));
    }
}
