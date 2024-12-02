// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Workspaces;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

public class ProjectStateGeneratedOutputTest(ITestOutputHelper testOutput) : WorkspaceTestBase(testOutput)
{
    private static readonly HostDocument s_hostDocument = TestProjectData.SomeProjectFile1;
    private static readonly HostProject s_hostProject = TestProjectData.SomeProject with { Configuration = FallbackRazorConfiguration.MVC_2_0 };
    private static readonly HostProject s_hostProjectWithConfigurationChange = TestProjectData.SomeProject with { Configuration = FallbackRazorConfiguration.MVC_1_0 };
    private static readonly ImmutableArray<TagHelperDescriptor> s_someTagHelpers = [TagHelperDescriptorBuilder.Create("Test1", "TestAssembly").Build()];

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
        builder.SetImportFeature(new TestImportProjectFeature());
    }

    [Fact]
    public async Task HostDocumentAdded_CachesOutput()
    {
        // Arrange
        var solutionState = SolutionState.Create(ProjectEngineFactoryProvider, LanguageServerFeatureOptions)
            .AddProject(s_hostProject)
            .AddDocument(s_hostProject.Key, s_hostDocument, DocumentState.EmptyLoader);

        var solution = new SolutionSnapshot(solutionState);
        var document = solution.GetRequiredDocument(s_hostProject.Key, s_hostDocument.FilePath);

        var (output, inputVersion) = await document.GetGeneratedOutputAndVersionAsync(DisposalToken);

        // Act
        solution = solution.AddDocument(s_hostProject.Key, TestProjectData.AnotherProjectFile1, DocumentState.EmptyLoader);
        document = solution.GetRequiredDocument(s_hostProject.Key, s_hostDocument.FilePath);

        var (actualOutput, actualInputVersion) = await document.GetGeneratedOutputAndVersionAsync(DisposalToken);

        // Assert
        Assert.Same(output, actualOutput);
        Assert.Equal(inputVersion, actualInputVersion);
    }

    [Fact]
    public async Task HostDocumentAdded_Import_DoesNotCacheOutput()
    {
        // Arrange
        var solutionState = SolutionState.Create(ProjectEngineFactoryProvider, LanguageServerFeatureOptions)
            .AddProject(s_hostProject)
            .AddDocument(s_hostProject.Key, s_hostDocument, DocumentState.EmptyLoader);

        var solution = new SolutionSnapshot(solutionState);
        var document = solution.GetRequiredDocument(s_hostProject.Key, s_hostDocument.FilePath);

        var (output, inputVersion) = await document.GetGeneratedOutputAndVersionAsync(DisposalToken);

        // Act
        solution = solution.AddDocument(s_hostProject.Key, TestProjectData.SomeProjectImportFile, DocumentState.EmptyLoader);
        document = solution.GetRequiredDocument(s_hostProject.Key, s_hostDocument.FilePath);

        var (actualOutput, actualInputVersion) = await document.GetGeneratedOutputAndVersionAsync(DisposalToken);

        // Assert
        Assert.NotSame(output, actualOutput);
        Assert.NotEqual(inputVersion, actualInputVersion);
        Assert.Equal(document.Project.DocumentCollectionVersion, actualInputVersion);
    }

    [Fact]
    public async Task HostDocumentChanged_DoesNotCacheOutput()
    {
        // Arrange
        var solutionState = SolutionState.Create(ProjectEngineFactoryProvider, LanguageServerFeatureOptions)
            .AddProject(s_hostProject)
            .AddDocument(s_hostProject.Key, s_hostDocument, DocumentState.EmptyLoader)
            .AddDocument(s_hostProject.Key, TestProjectData.SomeProjectImportFile, DocumentState.EmptyLoader);

        var solution = new SolutionSnapshot(solutionState);
        var document = solution.GetRequiredDocument(s_hostProject.Key, s_hostDocument.FilePath);

        var (output, inputVersion) = await document.GetGeneratedOutputAndVersionAsync(DisposalToken);

        // Act
        var version = VersionStamp.Create();
        solution = solution.UpdateDocumentText(s_hostProject.Key, s_hostDocument.FilePath, TestMocks.CreateTextLoader("@using System", version));
        document = solution.GetRequiredDocument(s_hostProject.Key, s_hostDocument.FilePath);

        var (actualOutput, actualInputVersion) = await document.GetGeneratedOutputAndVersionAsync(DisposalToken);

        // Assert
        Assert.NotSame(output, actualOutput);
        Assert.NotEqual(inputVersion, actualInputVersion);
        Assert.Equal(version, actualInputVersion);
    }

    [Fact]
    public async Task HostDocumentChanged_Import_DoesNotCacheOutput()
    {
        // Arrange
        var solutionState = SolutionState.Create(ProjectEngineFactoryProvider, LanguageServerFeatureOptions)
            .AddProject(s_hostProject)
            .AddDocument(s_hostProject.Key, s_hostDocument, DocumentState.EmptyLoader)
            .AddDocument(s_hostProject.Key, TestProjectData.SomeProjectImportFile, DocumentState.EmptyLoader);

        var solution = new SolutionSnapshot(solutionState);
        var document = solution.GetRequiredDocument(s_hostProject.Key, s_hostDocument.FilePath);

        var (output, inputVersion) = await document.GetGeneratedOutputAndVersionAsync(DisposalToken);

        // Act
        var version = VersionStamp.Create();
        solution = solution.UpdateDocumentText(s_hostProject.Key, TestProjectData.SomeProjectImportFile.FilePath, TestMocks.CreateTextLoader("@using System", version));
        document = solution.GetRequiredDocument(s_hostProject.Key, s_hostDocument.FilePath);

        var (actualOutput, actualInputVersion) = await document.GetGeneratedOutputAndVersionAsync(DisposalToken);

        // Assert
        Assert.NotSame(output, actualOutput);
        Assert.NotEqual(inputVersion, actualInputVersion);
        Assert.Equal(version, actualInputVersion);
    }

    [Fact]
    public async Task HostDocumentRemoved_Import_DoesNotCacheOutput()
    {
        // Arrange
        var solutionState = SolutionState.Create(ProjectEngineFactoryProvider, LanguageServerFeatureOptions)
            .AddProject(s_hostProject)
            .AddDocument(s_hostProject.Key, s_hostDocument, DocumentState.EmptyLoader)
            .AddDocument(s_hostProject.Key, TestProjectData.SomeProjectImportFile, DocumentState.EmptyLoader);

        var solution = new SolutionSnapshot(solutionState);
        var document = solution.GetRequiredDocument(s_hostProject.Key, s_hostDocument.FilePath);

        var (output, inputVersion) = await document.GetGeneratedOutputAndVersionAsync(DisposalToken);

        // Act
        solution = solution.RemoveDocument(s_hostProject.Key, TestProjectData.SomeProjectImportFile.FilePath);
        document = solution.GetRequiredDocument(s_hostProject.Key, s_hostDocument.FilePath);

        var (actualOutput, actualInputVersion) = await document.GetGeneratedOutputAndVersionAsync(DisposalToken);

        // Assert
        Assert.NotSame(output, actualOutput);
        Assert.NotEqual(inputVersion, actualInputVersion);
        Assert.Equal(document.Project.DocumentCollectionVersion, actualInputVersion);
    }

    [Fact]
    public async Task ProjectWorkspaceStateChange_CachesOutput_EvenWhenNewerProjectWorkspaceState()
    {
        // Arrange
        var solutionState = SolutionState.Create(ProjectEngineFactoryProvider, LanguageServerFeatureOptions)
            .AddProject(s_hostProject)
            .AddDocument(s_hostProject.Key, s_hostDocument, DocumentState.EmptyLoader);

        var solution = new SolutionSnapshot(solutionState);
        var document = solution.GetRequiredDocument(s_hostProject.Key, s_hostDocument.FilePath);

        var (output, inputVersion) = await document.GetGeneratedOutputAndVersionAsync(DisposalToken);

        // Act
        solution = solution.UpdateProjectWorkspaceState(s_hostProject.Key, ProjectWorkspaceState.Default);
        document = solution.GetRequiredDocument(s_hostProject.Key, s_hostDocument.FilePath);

        var (actualOutput, actualInputVersion) = await document.GetGeneratedOutputAndVersionAsync(DisposalToken);

        // Assert
        Assert.Same(output, actualOutput);
        Assert.Equal(inputVersion, actualInputVersion);
    }

    [Fact]
    public async Task ProjectWorkspaceStateChange_WithTagHelperChange_DoesNotCacheOutput()
    {
        // The generated code's text doesn't change as a result, so the output version does not change

        // Arrange
        var solutionState = SolutionState.Create(ProjectEngineFactoryProvider, LanguageServerFeatureOptions)
            .AddProject(s_hostProject)
            .AddDocument(s_hostProject.Key, s_hostDocument, DocumentState.EmptyLoader);

        var solution = new SolutionSnapshot(solutionState);
        var document = solution.GetRequiredDocument(s_hostProject.Key, s_hostDocument.FilePath);

        var (output, inputVersion) = await document.GetGeneratedOutputAndVersionAsync(DisposalToken);

        // Act
        var newProjectWorkspaceState = ProjectWorkspaceState.Create(s_someTagHelpers);
        solution = solution.UpdateProjectWorkspaceState(s_hostProject.Key, newProjectWorkspaceState);
        document = solution.GetRequiredDocument(s_hostProject.Key, s_hostDocument.FilePath);

        var (actualOutput, actualInputVersion) = await document.GetGeneratedOutputAndVersionAsync(DisposalToken);

        // Assert
        Assert.NotSame(output, actualOutput);
        Assert.NotEqual(inputVersion, actualInputVersion);
        Assert.Equal(document.Project.ProjectWorkspaceStateVersion, actualInputVersion);
    }

    [Fact]
    public async Task ProjectWorkspaceStateChange_WithProjectWorkspaceState_CSharpLanguageVersionChange_DoesNotCacheOutput()
    {
        // Arrange
        var hostProject = TestProjectData.SomeProject with
        {
            Configuration = s_hostProject.Configuration with
            {
                LanguageVersion = RazorLanguageVersion.Version_3_0
            }
        };

        var projectWorkspaceState = ProjectWorkspaceState.Create(s_someTagHelpers, LanguageVersion.CSharp7);

        var solutionState = SolutionState.Create(ProjectEngineFactoryProvider, LanguageServerFeatureOptions)
            .AddProject(hostProject)
            .UpdateProjectWorkspaceState(hostProject.Key, projectWorkspaceState)
            .AddDocument(s_hostProject.Key, s_hostDocument, TestMocks.CreateTextLoader("@DateTime.Now", VersionStamp.Default));

        var solution = new SolutionSnapshot(solutionState);
        var document = solution.GetRequiredDocument(s_hostProject.Key, s_hostDocument.FilePath);

        var (originalOutput, originalInputVersion) = await document.GetGeneratedOutputAndVersionAsync(DisposalToken);

        // Act
        var newProjectWorkspaceState = ProjectWorkspaceState.Create(s_someTagHelpers, LanguageVersion.CSharp8);
        solution = solution.UpdateProjectWorkspaceState(hostProject.Key, newProjectWorkspaceState);
        document = solution.GetRequiredDocument(s_hostProject.Key, s_hostDocument.FilePath);

        var (actualOutput, actualInputVersion) = await document.GetGeneratedOutputAndVersionAsync(DisposalToken);

        // Assert
        Assert.NotSame(originalOutput, actualOutput);
        Assert.NotEqual(originalInputVersion, actualInputVersion);
        Assert.Equal(document.Project.ProjectWorkspaceStateVersion, actualInputVersion);
    }

    [Fact]
    public async Task ConfigurationChange_DoesNotCacheOutput()
    {
        // Arrange

        var solutionState = SolutionState.Create(ProjectEngineFactoryProvider, LanguageServerFeatureOptions)
            .AddProject(s_hostProject)
            .AddDocument(s_hostProject.Key, s_hostDocument, DocumentState.EmptyLoader);

        var solution = new SolutionSnapshot(solutionState);
        var document = solution.GetRequiredDocument(s_hostProject.Key, s_hostDocument.FilePath);

        var (originalOutput, originalInputVersion) = await document.GetGeneratedOutputAndVersionAsync(DisposalToken);

        // Act
        solution = solution.UpdateProjectConfiguration(s_hostProjectWithConfigurationChange);
        document = solution.GetRequiredDocument(s_hostProject.Key, s_hostDocument.FilePath);

        var (actualOutput, actualInputVersion) = await document.GetGeneratedOutputAndVersionAsync(DisposalToken);

        // Assert
        Assert.NotSame(originalOutput, actualOutput);
        Assert.NotEqual(originalInputVersion, actualInputVersion);
        Assert.NotEqual(document.Project.ProjectWorkspaceStateVersion, actualInputVersion);
    }
}
