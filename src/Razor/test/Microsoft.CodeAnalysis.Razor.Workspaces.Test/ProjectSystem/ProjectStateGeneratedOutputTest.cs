// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.ProjectSystem;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

public class ProjectStateGeneratedOutputTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private static readonly IProjectEngineFactoryProvider s_projectEngineFactoryProvider = new TestProjectEngineFactoryProvider(b => b.SetImportFeature(TestImportProjectFeature.Instance));

    private static readonly HostProject s_project = TestProjectData.SomeProject with { Configuration = FallbackRazorConfiguration.MVC_2_0 };

    private static readonly HostDocument s_document1 = TestProjectData.SomeProjectFile1;
    private static readonly HostDocument s_document2 = TestProjectData.AnotherProjectFile1;
    private static readonly HostDocument s_importDocument = TestProjectData.SomeProjectImportFile;

    private static readonly ImmutableArray<TagHelperDescriptor> s_someTagHelpers = [TagHelperDescriptorBuilder.Create("Test1", "TestAssembly").Build()];

    [Fact]
    public async Task DocumentAdded_DoesNotCacheOutput()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectEngineFactoryProvider)
            .AddDocument(s_document1, EmptyTextLoader.Instance);

        var result = await GetOutputAsync(state, s_document1, DisposalToken);

        // Act
        var newState = state.AddDocument(s_document2, EmptyTextLoader.Instance);
        var newResult = await GetOutputAsync(newState, s_document1, DisposalToken);

        // Assert
        Assert.NotSame(result.Output, newResult.Output);
        Assert.NotEqual(result.Version, newResult.Version);
    }

    [Fact]
    public async Task DocumentChanged_DoesNotCacheOutput()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectEngineFactoryProvider)
            .AddDocument(s_document1, EmptyTextLoader.Instance)
            .AddDocument(s_importDocument, EmptyTextLoader.Instance);

        var result = await GetOutputAsync(state, s_document1, DisposalToken);

        // Act
        var version = VersionStamp.Create();
        var newState = state.WithDocumentText(s_document1, TestMocks.CreateTextLoader("@using System", version));
        var newResult = await GetOutputAsync(newState, s_document1, DisposalToken);

        // Assert
        Assert.NotSame(result.Output, newResult.Output);
        Assert.NotEqual(result.Version, newResult.Version);
        Assert.Equal(version, newResult.Version);
    }

    [Fact]
    public async Task DocumentRemoved_DoesNotCacheOutput()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectEngineFactoryProvider)
            .AddDocument(s_document1, EmptyTextLoader.Instance)
            .AddDocument(s_document2, EmptyTextLoader.Instance);

        var result = await GetOutputAsync(state, s_document1, DisposalToken);

        // Act
        var newState = state.RemoveDocument(s_document2);
        var newResult = await GetOutputAsync(newState, s_document1, DisposalToken);

        // Assert
        Assert.NotSame(result.Output, newResult.Output);
        Assert.NotEqual(result.Version, newResult.Version);
        Assert.Equal(newState.DocumentCollectionVersion, newResult.Version);
    }

    [Fact]
    public async Task ImportDocumentAdded_DoesNotCacheOutput()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectEngineFactoryProvider)
            .AddDocument(s_document1, EmptyTextLoader.Instance);

        var result = await GetOutputAsync(state, s_document1, DisposalToken);

        // Act
        var newState = state.AddDocument(s_importDocument, EmptyTextLoader.Instance);
        var newResult = await GetOutputAsync(newState, s_document1, DisposalToken);

        // Assert
        Assert.NotSame(result.Output, newResult.Output);
        Assert.NotEqual(result.Version, newResult.Version);
    }

    [Fact]
    public async Task ImportDocumentChanged_DoesNotCacheOutput()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectEngineFactoryProvider)
            .AddDocument(s_document1, EmptyTextLoader.Instance)
            .AddDocument(s_importDocument, EmptyTextLoader.Instance);

        var result = await GetOutputAsync(state, s_document1, DisposalToken);

        // Act
        var version = VersionStamp.Create();
        var newState = state.WithDocumentText(s_importDocument, TestMocks.CreateTextLoader("@using System", version));
        var newResult = await GetOutputAsync(newState, s_document1, DisposalToken);

        // Assert
        Assert.NotSame(result.Output, newResult.Output);
        Assert.NotEqual(result.Version, newResult.Version);
        Assert.Equal(version, newResult.Version);
    }

    [Fact]
    public async Task ImportDocumentRemoved_DoesNotCacheOutput()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectEngineFactoryProvider)
            .AddDocument(s_document1, EmptyTextLoader.Instance)
            .AddDocument(s_importDocument, EmptyTextLoader.Instance);

        var result = await GetOutputAsync(state, s_document1, DisposalToken);

        // Act
        var newState = state.RemoveDocument(s_importDocument);
        var newResult = await GetOutputAsync(newState, s_document1, DisposalToken);

        // Assert
        Assert.NotSame(result.Output, newResult.Output);
        Assert.NotEqual(result.Version, newResult.Version);
        Assert.Equal(newState.DocumentCollectionVersion, newResult.Version);
    }

    [Fact]
    public async Task ProjectWorkspaceStateChange_SameProjectWorkspaceState_CachesOutput()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectEngineFactoryProvider)
            .AddDocument(s_document1, EmptyTextLoader.Instance);

        var result = await GetOutputAsync(state, s_document1, DisposalToken);

        // Act
        var newState = state.WithProjectWorkspaceState(ProjectWorkspaceState.Default);
        var newResult = await GetOutputAsync(newState, s_document1, DisposalToken);

        // Assert
        Assert.Same(result.Output, newResult.Output);
        Assert.Equal(result.Version, newResult.Version);
    }

    [Fact]
    public async Task ProjectWorkspaceStateChange_TagHelpers_DoesNotCacheOutput()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectEngineFactoryProvider)
            .AddDocument(s_document1, EmptyTextLoader.Instance);

        var result = await GetOutputAsync(state, s_document1, DisposalToken);

        // Act
        var newState = state.WithProjectWorkspaceState(ProjectWorkspaceState.Create(s_someTagHelpers));
        var newResult = await GetOutputAsync(newState, s_document1, DisposalToken);

        // Assert
        Assert.NotSame(result.Output, newResult.Output);
        Assert.NotEqual(result.Version, newResult.Version);
        Assert.Equal(newState.ProjectWorkspaceStateVersion, newResult.Version);
    }

    [Fact]
    public async Task ProjectWorkspaceStateChange_WithProjectWorkspaceState_CSharpLanguageVersion_DoesNotCacheOutput()
    {
        // Arrange
        var hostProject = TestProjectData.SomeProject with
        {
            Configuration = s_project.Configuration with
            {
                LanguageVersion = RazorLanguageVersion.Version_3_0
            }
        };

        var state = ProjectState
            .Create(hostProject, ProjectWorkspaceState.Create(s_someTagHelpers, LanguageVersion.CSharp7), s_projectEngineFactoryProvider)
            .AddDocument(s_document1, TestMocks.CreateTextLoader("@DateTime.Now", VersionStamp.Default));

        var result = await GetOutputAsync(state, s_document1, DisposalToken);

        // Act
        var newState = state.WithProjectWorkspaceState(ProjectWorkspaceState.Create(s_someTagHelpers, LanguageVersion.CSharp8));
        var newResult = await GetOutputAsync(newState, s_document1, DisposalToken);

        // Assert
        Assert.NotSame(result.Output, newResult.Output);
        Assert.NotEqual(result.Version, newResult.Version);
        Assert.Equal(newState.ProjectWorkspaceStateVersion, newResult.Version);
    }

    [Fact]
    public async Task ConfigurationChange_DoesNotCacheOutput()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectEngineFactoryProvider)
            .AddDocument(s_document1, EmptyTextLoader.Instance);

        var result = await GetOutputAsync(state, s_document1, DisposalToken);

        // Act
        var newState = state.WithConfiguration(FallbackRazorConfiguration.MVC_1_0);
        var newResult = await GetOutputAsync(newState, s_document1, DisposalToken);

        // Assert
        Assert.NotSame(result.Output, newResult.Output);
        Assert.NotEqual(result.Version, newResult.Version);
        Assert.NotEqual(newState.ProjectWorkspaceStateVersion, newResult.Version);
    }

    private static Task<OutputAndVersion> GetOutputAsync(ProjectState project, HostDocument hostDocument, CancellationToken cancellationToken)
    {
        var document = project.Documents[hostDocument.FilePath];
        return GetOutputAsync(project, document, cancellationToken);
    }

    private static Task<OutputAndVersion> GetOutputAsync(ProjectState project, DocumentState document, CancellationToken cancellationToken)
    {
        var projectSnapshot = new ProjectSnapshot(project);
        var documentSnapshot = new DocumentSnapshot(projectSnapshot, document);
        return documentSnapshot.GetGeneratedOutputAndVersionAsync(cancellationToken);
    }
}
