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

public class RazorProjectGeneratedOutputTest : WorkspaceTestBase
{
    private readonly HostDocument _hostDocument;
    private readonly HostProject _hostProject;
    private readonly HostProject _hostProjectWithConfigurationChange;
    private readonly ImmutableArray<TagHelperDescriptor> _someTagHelpers;

    public RazorProjectGeneratedOutputTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        _hostProject = TestProjectData.SomeProject with { Configuration = FallbackRazorConfiguration.MVC_2_0 };
        _hostProjectWithConfigurationChange = TestProjectData.SomeProject with { Configuration = FallbackRazorConfiguration.MVC_1_0 };

        _someTagHelpers = [TagHelperDescriptorBuilder.Create("Test1", "TestAssembly").Build()];

        _hostDocument = TestProjectData.SomeProjectFile1;
    }

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
        builder.SetImportFeature(new TestImportProjectFeature());
    }

    [Fact]
    public async Task AddDocument_CachesOutput()
    {
        // Arrange
        var project = RazorProject
            .Create(_hostProject, CompilerOptions, ProjectEngineFactoryProvider)
            .AddEmptyDocument(_hostDocument);

        var output = await GetGeneratedOutputAsync(project);

        // Act
        var newProject = project.AddEmptyDocument(TestProjectData.AnotherProjectFile1);
        var newOutput = await GetGeneratedOutputAsync(newProject);

        // Assert
        Assert.Same(output, newOutput);
    }

    [Fact]
    public async Task AddDocument_Import_DoesNotCacheOutput()
    {
        // Arrange
        var project = RazorProject
            .Create(_hostProject, CompilerOptions, ProjectEngineFactoryProvider)
            .AddEmptyDocument(_hostDocument);

        var output = await GetGeneratedOutputAsync(project);

        // Act
        var newProject = project.AddEmptyDocument(TestProjectData.SomeProjectImportFile);
        var newOutput = await GetGeneratedOutputAsync(newProject);

        // Assert
        Assert.NotSame(output, newOutput);
    }

    [Fact]
    public async Task WithDocumentText_DoesNotCacheOutput()
    {
        // Arrange
        var project = RazorProject
            .Create(_hostProject, CompilerOptions, ProjectEngineFactoryProvider)
            .AddEmptyDocument(_hostDocument)
            .AddEmptyDocument(TestProjectData.SomeProjectImportFile);

        var output = await GetGeneratedOutputAsync(project);

        // Act
        var newProject = project.WithDocumentText(_hostDocument.FilePath, TestMocks.CreateTextLoader("@using System"));
        var newOutput = await GetGeneratedOutputAsync(newProject);

        // Assert
        Assert.NotSame(output, newOutput);
    }

    [Fact]
    public async Task WithDocumentText_Import_DoesNotCacheOutput()
    {
        // Arrange
        var project = RazorProject
            .Create(_hostProject, CompilerOptions, ProjectEngineFactoryProvider)
            .AddEmptyDocument(_hostDocument)
            .AddEmptyDocument(TestProjectData.SomeProjectImportFile);

        var output = await GetGeneratedOutputAsync(project);

        // Act
        var newProject = project.WithDocumentText(TestProjectData.SomeProjectImportFile.FilePath, TestMocks.CreateTextLoader("@using System"));
        var newOutput = await GetGeneratedOutputAsync(newProject);

        // Assert
        Assert.NotSame(output, newOutput);
    }

    [Fact]
    public async Task RemoveDocument_Import_DoesNotCacheOutput()
    {
        // Arrange
        var project = RazorProject
            .Create(_hostProject, CompilerOptions, ProjectEngineFactoryProvider)
            .AddEmptyDocument(_hostDocument)
            .AddEmptyDocument(TestProjectData.SomeProjectImportFile);

        var output = await GetGeneratedOutputAsync(project);

        // Act
        var newProject = project.RemoveDocument(TestProjectData.SomeProjectImportFile.FilePath);
        var newOutput = await GetGeneratedOutputAsync(newProject);

        // Assert
        Assert.NotSame(output, newOutput);
    }

    [Fact]
    public async Task WithProjectWorkspaceState_CachesOutput_EvenWhenNewerProjectWorkspaceState()
    {
        // Arrange
        var project = RazorProject
            .Create(_hostProject, CompilerOptions, ProjectEngineFactoryProvider)
            .AddEmptyDocument(_hostDocument);

        var output = await GetGeneratedOutputAsync(project);

        // Act
        var newProject = project.WithProjectWorkspaceState(ProjectWorkspaceState.Default);
        var newOutput = await GetGeneratedOutputAsync(newProject);

        // Assert
        Assert.Same(output, newOutput);
    }

    [Fact]
    public async Task WithProjectWorkspaceState_TagHelperChange_DoesNotCacheOutput()
    {
        // Arrange
        var project = RazorProject
            .Create(_hostProject, CompilerOptions, ProjectEngineFactoryProvider)
            .AddEmptyDocument(_hostDocument);

        var output = await GetGeneratedOutputAsync(project);

        // Act
        var newProject = project.WithProjectWorkspaceState(ProjectWorkspaceState.Create(_someTagHelpers));
        var newOutput = await GetGeneratedOutputAsync(newProject);

        // Assert
        Assert.NotSame(output, newOutput);
    }

    [Fact]
    public async Task WithProjectWorkspaceState_CSharpLanguageVersionChange_DoesNotCacheOutput()
    {
        // Arrange
        var hostProject = TestProjectData.SomeProject with
        {
            Configuration = _hostProject.Configuration with { LanguageVersion = RazorLanguageVersion.Version_3_0 }
        };

        var projectWorkspaceState = ProjectWorkspaceState.Create(_someTagHelpers, LanguageVersion.CSharp7);

        var project = RazorProject
            .Create(hostProject, CompilerOptions, ProjectEngineFactoryProvider)
            .WithProjectWorkspaceState(projectWorkspaceState)
            .AddDocument(_hostDocument, TestMocks.CreateTextLoader("@DateTime.Now", VersionStamp.Default));

        var output = await GetGeneratedOutputAsync(project);

        // Act
        var newProjectWorkspaceState = ProjectWorkspaceState.Create(_someTagHelpers, LanguageVersion.CSharp8);
        var newProject = project.WithProjectWorkspaceState(newProjectWorkspaceState);
        var newOutput = await GetGeneratedOutputAsync(newProject);

        // Assert
        Assert.NotSame(output, newOutput);
    }

    [Fact]
    public async Task WithHostProject_DoesNotCacheOutput()
    {
        // Arrange
        var project = RazorProject
            .Create(_hostProject, CompilerOptions, ProjectEngineFactoryProvider)
            .AddEmptyDocument(_hostDocument);

        var output = await GetGeneratedOutputAsync(project);

        // Act
        var newProject = project.WithHostProject(_hostProjectWithConfigurationChange);
        var newOutput = await GetGeneratedOutputAsync(newProject);

        // Assert
        Assert.NotSame(output, newOutput);
    }

    private ValueTask<RazorCodeDocument> GetGeneratedOutputAsync(RazorProject project)
    {
        var document = project.GetRequiredDocument(_hostDocument.FilePath);

        return document.GetGeneratedOutputAsync(DisposalToken);
    }
}
