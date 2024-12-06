// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.ProjectSystem;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

public class ProjectStateTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private static readonly IProjectEngineFactoryProvider s_projectEngineFactoryProvider = new TestProjectEngineFactoryProvider(b => b.SetImportFeature(TestImportProjectFeature.Instance));

    private static readonly HostProject s_project = TestProjectData.SomeProject with { Configuration = FallbackRazorConfiguration.MVC_2_0 };
    private static readonly HostProject s_projectWithConfigurationChange = TestProjectData.SomeProject with { Configuration = FallbackRazorConfiguration.MVC_1_0 };
    private static readonly ProjectWorkspaceState s_projectWorkspaceState = ProjectWorkspaceState.Create([TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly").Build()]);

    private static readonly HostDocument[] s_documents =
    [
        TestProjectData.SomeProjectFile1,
        TestProjectData.SomeProjectFile2,

        // linked file
        TestProjectData.AnotherProjectNestedFile3
    ];

    private static readonly SourceText s_text = SourceText.From("Hello, world!");
    private static readonly TextAndVersion s_textAndVersion = TextAndVersion.Create(s_text, VersionStamp.Create());
    private static readonly TextLoader s_textLoader = TestMocks.CreateTextLoader(s_textAndVersion);

    [Fact]
    public void GetImportDocumentTargetPaths_DoesNotIncludeCurrentImport()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(TestProjectData.SomeProjectComponentImportFile1, EmptyTextLoader.Instance);

        // Act
        var paths = state.GetImportDocumentTargetPaths(TestProjectData.SomeProjectComponentImportFile1);

        // Assert
        Assert.DoesNotContain(TestProjectData.SomeProjectComponentImportFile1.TargetPath, paths);
    }

    [Fact]
    public void ProjectState_ConstructedNew()
    {
        // Arrange & act
        var state = ProjectState.Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider);

        // Assert
        Assert.Empty(state.Documents);
        Assert.NotEqual(VersionStamp.Default, state.Version);
    }

    [Fact]
    public void ProjectState_AddHostDocument_ToEmpty()
    {
        // Arrange
        var state = ProjectState.Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider);

        // Act
        var newState = state.AddDocument(s_documents[0], EmptyTextLoader.Instance);

        // Assert
        Assert.NotEqual(state.Version, newState.Version);

        Assert.Single(newState.Documents, static d => ReferenceEquals(s_documents[0], d.Value.HostDocument));
        Assert.NotEqual(state.DocumentCollectionVersion, newState.DocumentCollectionVersion);
    }

    [Fact]
    public async Task ProjectState_AddHostDocument_DocumentIsEmpty()
    {
        // Arrange
        var state = ProjectState.Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider);

        // Act
        var newState = state.AddDocument(s_documents[0], EmptyTextLoader.Instance);

        // Assert
        var text = await newState.Documents[s_documents[0].FilePath].GetTextAsync(DisposalToken);
        Assert.Equal(0, text.Length);
    }

    [Fact]
    public void ProjectState_AddHostDocument_ToProjectWithDocuments()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(s_documents[2], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance);

        // Act
        var newState = state.AddDocument(s_documents[0], EmptyTextLoader.Instance);

        // Assert
        Assert.NotEqual(state.Version, newState.Version);

        Assert.Collection(
            newState.Documents.OrderBy(static kvp => kvp.Key),
            d => Assert.Same(s_documents[2], d.Value.HostDocument),
            d => Assert.Same(s_documents[0], d.Value.HostDocument),
            d => Assert.Same(s_documents[1], d.Value.HostDocument));

        Assert.NotEqual(state.DocumentCollectionVersion, newState.DocumentCollectionVersion);
    }

    [Fact]
    public void ProjectState_AddHostDocument_TracksImports()
    {
        // Arrange & Act
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(TestProjectData.SomeProjectFile1, EmptyTextLoader.Instance)
            .AddDocument(TestProjectData.SomeProjectFile2, EmptyTextLoader.Instance)
            .AddDocument(TestProjectData.SomeProjectNestedFile3, EmptyTextLoader.Instance)
            .AddDocument(TestProjectData.AnotherProjectNestedFile4, EmptyTextLoader.Instance);

        // Assert
        Assert.Collection(
            state.ImportsToRelatedDocuments.OrderBy(static kvp => kvp.Key),
            static kvp =>
            {
                Assert.Equal(TestProjectData.SomeProjectImportFile.TargetPath, kvp.Key);
                Assert.Equal(
                    [
                        TestProjectData.AnotherProjectNestedFile4.FilePath,
                        TestProjectData.SomeProjectFile1.FilePath,
                        TestProjectData.SomeProjectFile2.FilePath,
                        TestProjectData.SomeProjectNestedFile3.FilePath,
                    ],
                    kvp.Value.OrderBy(static f => f));
            },
            static kvp =>
            {
                Assert.Equal(TestProjectData.SomeProjectNestedImportFile.TargetPath, kvp.Key);
                Assert.Equal(
                    [
                        TestProjectData.AnotherProjectNestedFile4.FilePath,
                        TestProjectData.SomeProjectNestedFile3.FilePath,
                    ],
                    kvp.Value.OrderBy(static f => f));
            });
    }

    [Fact]
    public void ProjectState_AddHostDocument_TracksImports_AddImportFile()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(TestProjectData.SomeProjectFile1, EmptyTextLoader.Instance)
            .AddDocument(TestProjectData.SomeProjectFile2, EmptyTextLoader.Instance)
            .AddDocument(TestProjectData.SomeProjectNestedFile3, EmptyTextLoader.Instance)
            .AddDocument(TestProjectData.AnotherProjectNestedFile4, EmptyTextLoader.Instance);

        // Act
        var newState = state.AddDocument(TestProjectData.AnotherProjectImportFile, EmptyTextLoader.Instance);

        // Assert
        Assert.Collection(
            newState.ImportsToRelatedDocuments.OrderBy(static kvp => kvp.Key),
            static kvp =>
            {
                Assert.Equal(TestProjectData.SomeProjectImportFile.TargetPath, kvp.Key);
                Assert.Equal(
                    [
                        TestProjectData.AnotherProjectNestedFile4.FilePath,
                        TestProjectData.SomeProjectFile1.FilePath,
                        TestProjectData.SomeProjectFile2.FilePath,
                        TestProjectData.SomeProjectNestedFile3.FilePath,
                    ],
                    kvp.Value.OrderBy(static f => f));
            },
            static kvp =>
            {
                Assert.Equal(TestProjectData.SomeProjectNestedImportFile.TargetPath, kvp.Key);
                Assert.Equal(
                    [
                        TestProjectData.AnotherProjectNestedFile4.FilePath,
                        TestProjectData.SomeProjectNestedFile3.FilePath,
                    ],
                    kvp.Value.OrderBy(static f => f));
            });
    }

    [Fact]
    public void ProjectState_AddHostDocument_RetainsComputedState()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(s_documents[2], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance);

        var tagHelpers = state.TagHelpers;
        var projectWorkspaceStateVersion = state.ProjectWorkspaceStateVersion;

        // Act
        var newState = state.AddDocument(s_documents[0], EmptyTextLoader.Instance);
        var newTagHelpers = newState.TagHelpers;
        var newProjectWorkspaceStateVersion = newState.ProjectWorkspaceStateVersion;

        // Assert
        Assert.Same(state.ProjectEngine, newState.ProjectEngine);

        Assert.Equal(tagHelpers.Length, newTagHelpers.Length);
        for (var i = 0; i < tagHelpers.Length; i++)
        {
            Assert.Same(tagHelpers[i], newTagHelpers[i]);
        }

        Assert.Equal(projectWorkspaceStateVersion, newProjectWorkspaceStateVersion);

        Assert.Same(state.Documents[s_documents[1].FilePath], newState.Documents[s_documents[1].FilePath]);
        Assert.Same(state.Documents[s_documents[2].FilePath], newState.Documents[s_documents[2].FilePath]);
    }

    [Fact]
    public void ProjectState_AddHostDocument_DuplicateIgnored()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(s_documents[2], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance);

        // Act
        var newState = state.AddDocument(new HostDocument(s_documents[1].FilePath, "SomePath.cshtml"), EmptyTextLoader.Instance);

        // Assert
        Assert.Same(state, newState);
    }

    [Fact]
    public async Task ProjectState_WithChangedHostDocument_Loader()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(s_documents[2], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance);

        // Act
        var newState = state.UpdateDocumentText(s_documents[1], s_textLoader);

        // Assert
        Assert.NotEqual(state.Version, newState.Version);

        var text = await newState.Documents[s_documents[1].FilePath].GetTextAsync(DisposalToken);
        Assert.Same(s_text, text);
        Assert.Equal(state.DocumentCollectionVersion, newState.DocumentCollectionVersion);
    }

    [Fact]
    public async Task ProjectState_WithChangedHostDocument_Snapshot()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(s_documents[2], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance);

        // Act
        var newState = state.UpdateDocumentText(s_documents[1], s_text, VersionStamp.Create());

        // Assert
        Assert.NotEqual(state.Version, newState.Version);

        var text = await newState.Documents[s_documents[1].FilePath].GetTextAsync(DisposalToken);
        Assert.Same(s_text, text);

        Assert.Equal(state.DocumentCollectionVersion, newState.DocumentCollectionVersion);
    }

    [Fact]
    public void ProjectState_WithChangedHostDocument_Loader_RetainsComputedState()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(s_documents[2], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance);

        var tagHelpers = state.TagHelpers;
        var projectWorkspaceStateVersion = state.ProjectWorkspaceStateVersion;

        // Act
        var newState = state.UpdateDocumentText(s_documents[1], s_textLoader);
        var newTagHelpers = newState.TagHelpers;
        var newProjectWorkspaceStateVersion = newState.ProjectWorkspaceStateVersion;

        // Assert
        Assert.Same(state.ProjectEngine, newState.ProjectEngine);

        Assert.Equal(tagHelpers.Length, newTagHelpers.Length);
        for (var i = 0; i < tagHelpers.Length; i++)
        {
            Assert.Same(tagHelpers[i], newTagHelpers[i]);
        }

        Assert.Equal(projectWorkspaceStateVersion, newProjectWorkspaceStateVersion);

        Assert.NotSame(state.Documents[s_documents[1].FilePath], newState.Documents[s_documents[1].FilePath]);
    }

    [Fact]
    public void ProjectState_WithChangedHostDocument_Snapshot_RetainsComputedState()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(s_documents[2], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance);

        var tagHelpers = state.TagHelpers;
        var projectWorkspaceStateVersion = state.ProjectWorkspaceStateVersion;

        // Act
        var newState = state.UpdateDocumentText(s_documents[1], s_text, VersionStamp.Create());
        var newTagHelpers = newState.TagHelpers;
        var newProjectWorkspaceStateVersion = newState.ProjectWorkspaceStateVersion;

        // Assert
        Assert.Same(state.ProjectEngine, newState.ProjectEngine);

        Assert.Equal(tagHelpers.Length, newTagHelpers.Length);
        for (var i = 0; i < tagHelpers.Length; i++)
        {
            Assert.Same(tagHelpers[i], newTagHelpers[i]);
        }

        Assert.Equal(projectWorkspaceStateVersion, newProjectWorkspaceStateVersion);

        Assert.NotSame(state.Documents[s_documents[1].FilePath], newState.Documents[s_documents[1].FilePath]);
    }

    [Fact]
    public void ProjectState_WithChangedHostDocument_Loader_NotFoundIgnored()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(s_documents[2], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance);

        // Act
        var newState = state.UpdateDocumentText(s_documents[0], s_textLoader);

        // Assert
        Assert.Same(state, newState);
    }

    [Fact]
    public void ProjectState_WithChangedHostDocument_Snapshot_NotFoundIgnored()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(s_documents[2], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance);

        // Act
        var newState = state.UpdateDocumentText(s_documents[0], s_text, VersionStamp.Create());

        // Assert
        Assert.Same(state, newState);
    }

    [Fact]
    public void ProjectState_RemoveHostDocument_FromProjectWithDocuments()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(s_documents[2], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance);

        // Act
        var newState = state.RemoveDocument(s_documents[1]);

        // Assert
        Assert.NotEqual(state.Version, newState.Version);

        Assert.Single(newState.Documents, d => ReferenceEquals(s_documents[2], d.Value.HostDocument));

        Assert.NotEqual(state.DocumentCollectionVersion, newState.DocumentCollectionVersion);
    }

    [Fact]
    public void ProjectState_RemoveHostDocument_TracksImports()
    {
        // Arrange
        var original = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(TestProjectData.SomeProjectFile1, EmptyTextLoader.Instance)
            .AddDocument(TestProjectData.SomeProjectFile2, EmptyTextLoader.Instance)
            .AddDocument(TestProjectData.SomeProjectNestedFile3, EmptyTextLoader.Instance)
            .AddDocument(TestProjectData.AnotherProjectNestedFile4, EmptyTextLoader.Instance);

        // Act
        var state = original.RemoveDocument(TestProjectData.SomeProjectNestedFile3);

        // Assert
        Assert.Collection(
            state.ImportsToRelatedDocuments.OrderBy(static kvp => kvp.Key),
            static kvp =>
            {
                Assert.Equal(TestProjectData.SomeProjectImportFile.TargetPath, kvp.Key);
                Assert.Equal(
                    [
                        TestProjectData.AnotherProjectNestedFile4.FilePath,
                        TestProjectData.SomeProjectFile1.FilePath,
                        TestProjectData.SomeProjectFile2.FilePath,
                    ],
                    kvp.Value.OrderBy(static f => f));
            },
            static kvp =>
            {
                Assert.Equal(TestProjectData.SomeProjectNestedImportFile.TargetPath, kvp.Key);
                Assert.Equal(
                    [
                        TestProjectData.AnotherProjectNestedFile4.FilePath,
                    ],
                    kvp.Value.OrderBy(static f => f));
            });
    }

    [Fact]
    public void ProjectState_RemoveHostDocument_TracksImports_RemoveAllDocuments()
    {
        // Arrange
        var state = ProjectState.Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(TestProjectData.SomeProjectFile1, EmptyTextLoader.Instance)
            .AddDocument(TestProjectData.SomeProjectFile2, EmptyTextLoader.Instance)
            .AddDocument(TestProjectData.SomeProjectNestedFile3, EmptyTextLoader.Instance)
            .AddDocument(TestProjectData.AnotherProjectNestedFile4, EmptyTextLoader.Instance);

        // Act
        var newState = state
            .RemoveDocument(TestProjectData.SomeProjectFile1)
            .RemoveDocument(TestProjectData.SomeProjectFile2)
            .RemoveDocument(TestProjectData.SomeProjectNestedFile3)
            .RemoveDocument(TestProjectData.AnotherProjectNestedFile4);

        // Assert
        Assert.Empty(newState.Documents);
        Assert.Empty(newState.ImportsToRelatedDocuments);
    }

    [Fact]
    public void ProjectState_RemoveHostDocument_RetainsComputedState()
    {
        // Arrange
        var state = ProjectState.Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(s_documents[2], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance);

        var tagHelpers = state.TagHelpers;
        var projectWorkspaceStateVersion = state.ProjectWorkspaceStateVersion;

        // Act
        var newState = state.RemoveDocument(s_documents[2]);
        var newTagHelpers = newState.TagHelpers;
        var newProjectWorkspaceStateVersion = newState.ProjectWorkspaceStateVersion;

        // Assert
        Assert.Same(state.ProjectEngine, newState.ProjectEngine);

        Assert.Equal(tagHelpers.Length, newTagHelpers.Length);
        for (var i = 0; i < tagHelpers.Length; i++)
        {
            Assert.Same(tagHelpers[i], newTagHelpers[i]);
        }

        Assert.Equal(projectWorkspaceStateVersion, newProjectWorkspaceStateVersion);

        Assert.Same(state.Documents[s_documents[1].FilePath], newState.Documents[s_documents[1].FilePath]);
    }

    [Fact]
    public void ProjectState_RemoveHostDocument_NotFoundIgnored()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(s_documents[2], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance);

        // Act
        var newState = state.RemoveDocument(s_documents[0]);

        // Assert
        Assert.Same(state, newState);
    }

    [Fact]
    public void ProjectState_WithHostProject_ConfigurationChange_UpdatesConfigurationState()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(s_documents[2], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance);

        var tagHelpers = state.TagHelpers;
        var projectWorkspaceStateVersion = state.ConfigurationVersion;

        // Act
        var newState = state.WithHostProject(s_projectWithConfigurationChange);
        Assert.NotEqual(state.Version, newState.Version);
        Assert.Same(s_projectWithConfigurationChange, newState.HostProject);

        // Assert
        var actualTagHelpers = newState.TagHelpers;
        var actualProjectWorkspaceStateVersion = newState.ConfigurationVersion;

        Assert.NotSame(state.ProjectEngine, newState.ProjectEngine);

        Assert.Equal(tagHelpers.Length, actualTagHelpers.Length);
        for (var i = 0; i < tagHelpers.Length; i++)
        {
            Assert.Same(tagHelpers[i], actualTagHelpers[i]);
        }

        Assert.NotEqual(projectWorkspaceStateVersion, actualProjectWorkspaceStateVersion);

        Assert.NotSame(state.Documents[s_documents[1].FilePath], newState.Documents[s_documents[1].FilePath]);
        Assert.NotSame(state.Documents[s_documents[2].FilePath], newState.Documents[s_documents[2].FilePath]);

        Assert.NotEqual(state.DocumentCollectionVersion, newState.DocumentCollectionVersion);
    }

    [Fact]
    public void ProjectState_WithHostProject_RootNamespaceChange_UpdatesConfigurationState()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(s_documents[2], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance);

        var hostProjectWithRootNamespaceChange = state.HostProject with { RootNamespace = "ChangedRootNamespace" };

        // Act
        var newState = state.WithHostProject(hostProjectWithRootNamespaceChange);

        // Assert
        Assert.NotSame(state, newState);
    }

    [Fact]
    public void ProjectState_WithHostProject_NoConfigurationChange_Ignored()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(s_documents[2], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance);

        // Act
        var newState = state.WithHostProject(s_project);

        // Assert
        Assert.Same(state, newState);
    }

    [Fact]
    public void ProjectState_WithHostProject_CallsConfigurationChangeOnDocumentState()
    {
        // Arrange
        var callCount = 0;

        var documents = ImmutableDictionary.CreateBuilder<string, DocumentState>(FilePathComparer.Instance);
        documents[s_documents[1].FilePath] = TestDocumentState.Create(s_documents[1], onConfigurationChange: () => callCount++);
        documents[s_documents[2].FilePath] = TestDocumentState.Create(s_documents[2], onConfigurationChange: () => callCount++);

        var state = ProjectState.Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider);
        state.Documents = documents.ToImmutable();

        // Act
        var newState = state.WithHostProject(s_projectWithConfigurationChange);

        // Assert
        Assert.NotEqual(state.Version, newState.Version);
        Assert.Same(s_projectWithConfigurationChange, newState.HostProject);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void ProjectState_WithHostProject_ResetsImportedDocuments()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(TestProjectData.SomeProjectFile1, EmptyTextLoader.Instance);

        // Act
        var newState = state.WithHostProject(s_projectWithConfigurationChange);

        // Assert
        var importMap = Assert.Single(newState.ImportsToRelatedDocuments);
        var documentFilePath = Assert.Single(importMap.Value);
        Assert.Equal(TestProjectData.SomeProjectFile1.FilePath, documentFilePath);
    }

    [Fact]
    public void ProjectState_WithProjectWorkspaceState_Changed()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(s_documents[2], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance);

        var tagHelpers = state.TagHelpers;
        var projectWorkspaceStateVersion = state.ProjectWorkspaceStateVersion;

        // Act
        var newProjectWorkspaceState = ProjectWorkspaceState.Create(s_projectWorkspaceState.TagHelpers, LanguageVersion.CSharp6);
        var newState = state.WithProjectWorkspaceState(newProjectWorkspaceState);

        // Assert
        Assert.NotEqual(state.Version, newState.Version);
        Assert.Same(newProjectWorkspaceState, newState.ProjectWorkspaceState);

        var actualTagHelpers = newState.TagHelpers;
        var actualProjectWorkspaceStateVersion = newState.ProjectWorkspaceStateVersion;

        // The C# language version changed, and the tag helpers didn't change
        Assert.NotSame(state.ProjectEngine, newState.ProjectEngine);

        Assert.Equal(tagHelpers.Length, actualTagHelpers.Length);
        for (var i = 0; i < tagHelpers.Length; i++)
        {
            Assert.Same(tagHelpers[i], actualTagHelpers[i]);
        }

        Assert.NotEqual(projectWorkspaceStateVersion, actualProjectWorkspaceStateVersion);

        Assert.NotSame(state.Documents[s_documents[1].FilePath], newState.Documents[s_documents[1].FilePath]);
        Assert.NotSame(state.Documents[s_documents[2].FilePath], newState.Documents[s_documents[2].FilePath]);
    }

    [Fact]
    public void ProjectState_WithProjectWorkspaceState_Changed_TagHelpersChanged()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(s_documents[2], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance);

        var tagHelpers = state.TagHelpers;
        var projectWorkspaceStateVersion = state.ProjectWorkspaceStateVersion;

        // Act
        var newState = state.WithProjectWorkspaceState(ProjectWorkspaceState.Default);

        // Assert
        Assert.NotEqual(state.Version, newState.Version);
        Assert.Same(ProjectWorkspaceState.Default, newState.ProjectWorkspaceState);

        var actualTagHelpers = newState.TagHelpers;
        var actualProjectWorkspaceStateVersion = newState.ProjectWorkspaceStateVersion;

        // The configuration didn't change, but the tag helpers did
        Assert.Same(state.ProjectEngine, newState.ProjectEngine);
        Assert.NotEqual(tagHelpers, actualTagHelpers);
        Assert.NotEqual(projectWorkspaceStateVersion, actualProjectWorkspaceStateVersion);
        Assert.Equal(newState.Version, actualProjectWorkspaceStateVersion);

        Assert.NotSame(state.Documents[s_documents[1].FilePath], newState.Documents[s_documents[1].FilePath]);
        Assert.NotSame(state.Documents[s_documents[2].FilePath], newState.Documents[s_documents[2].FilePath]);
    }

    [Fact]
    public void ProjectState_WithProjectWorkspaceState_IdenticalState_Caches()
    {
        // Arrange
        var state = ProjectState
            .Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider)
            .AddDocument(s_documents[2], EmptyTextLoader.Instance)
            .AddDocument(s_documents[1], EmptyTextLoader.Instance);

        // Act
        var newProjectWorkspaceState = ProjectWorkspaceState.Create(state.TagHelpers, state.CSharpLanguageVersion);
        var newState = state.WithProjectWorkspaceState(newProjectWorkspaceState);

        // Assert
        Assert.Same(state, newState);
    }

    [Fact]
    public void ProjectState_WithProjectWorkspaceState_CallsWorkspaceProjectChangeOnDocumentState()
    {
        // Arrange
        var callCount = 0;

        var documents = ImmutableDictionary.CreateBuilder<string, DocumentState>(FilePathComparer.Instance);
        documents[s_documents[1].FilePath] = TestDocumentState.Create(s_documents[1], onProjectWorkspaceStateChange: () => callCount++);
        documents[s_documents[2].FilePath] = TestDocumentState.Create(s_documents[2], onProjectWorkspaceStateChange: () => callCount++);

        var state = ProjectState.Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider);
        state.Documents = documents.ToImmutable();

        // Act
        var newState = state.WithProjectWorkspaceState(ProjectWorkspaceState.Default);

        // Assert
        Assert.NotEqual(state.Version, newState.Version);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void ProjectState_WhenImportDocumentAdded_CallsImportsChanged()
    {
        // Arrange
        var callCount = 0;

        var document1 = TestProjectData.SomeProjectFile1;
        var document2 = TestProjectData.SomeProjectFile2;
        var document3 = TestProjectData.SomeProjectNestedFile3;
        var document4 = TestProjectData.AnotherProjectNestedFile4;

        var documents = ImmutableDictionary.CreateBuilder<string, DocumentState>(FilePathComparer.Instance);
        documents[document1.FilePath] = TestDocumentState.Create(document1, onImportsChange: () => callCount++);
        documents[document2.FilePath] = TestDocumentState.Create(document2, onImportsChange: () => callCount++);
        documents[document3.FilePath] = TestDocumentState.Create(document3, onImportsChange: () => callCount++);
        documents[document4.FilePath] = TestDocumentState.Create(document4, onImportsChange: () => callCount++);

        var importsToRelatedDocuments = ImmutableDictionary.CreateBuilder<string, ImmutableArray<string>>(FilePathComparer.Instance);
        importsToRelatedDocuments.Add(
            TestProjectData.SomeProjectImportFile.TargetPath,
            [
                TestProjectData.SomeProjectFile1.FilePath,
                TestProjectData.SomeProjectFile2.FilePath,
                TestProjectData.SomeProjectNestedFile3.FilePath,
                TestProjectData.AnotherProjectNestedFile4.FilePath,
            ]);
        importsToRelatedDocuments.Add(
            TestProjectData.SomeProjectNestedImportFile.TargetPath,
            [
                TestProjectData.SomeProjectNestedFile3.FilePath,
                TestProjectData.AnotherProjectNestedFile4.FilePath
            ]);

        var state = ProjectState.Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider);
        state.Documents = documents.ToImmutable();
        state.ImportsToRelatedDocuments = importsToRelatedDocuments.ToImmutable();

        // Act
        var newState = state.AddDocument(TestProjectData.AnotherProjectImportFile, EmptyTextLoader.Instance);

        // Assert
        Assert.NotEqual(state.Version, newState.Version);
        Assert.Equal(4, callCount);
    }

    [Fact]
    public void ProjectState_WhenImportDocumentAdded_CallsImportsChanged_Nested()
    {
        // Arrange
        var callCount = 0;

        var document1 = TestProjectData.SomeProjectFile1;
        var document2 = TestProjectData.SomeProjectFile2;
        var document3 = TestProjectData.SomeProjectNestedFile3;
        var document4 = TestProjectData.AnotherProjectNestedFile4;

        var documents = ImmutableDictionary.CreateBuilder<string, DocumentState>(FilePathComparer.Instance);
        documents[document1.FilePath] = TestDocumentState.Create(document1, onImportsChange: () => callCount++);
        documents[document2.FilePath] = TestDocumentState.Create(document2, onImportsChange: () => callCount++);
        documents[document3.FilePath] = TestDocumentState.Create(document3, onImportsChange: () => callCount++);
        documents[document4.FilePath] = TestDocumentState.Create(document4, onImportsChange: () => callCount++);

        var importsToRelatedDocuments = ImmutableDictionary.CreateBuilder<string, ImmutableArray<string>>(FilePathComparer.Instance);
        importsToRelatedDocuments.Add(
            TestProjectData.SomeProjectImportFile.TargetPath,
            [
                TestProjectData.SomeProjectFile1.FilePath,
                TestProjectData.SomeProjectFile2.FilePath,
                TestProjectData.SomeProjectNestedFile3.FilePath,
                TestProjectData.AnotherProjectNestedFile4.FilePath,
            ]);
        importsToRelatedDocuments.Add(
            TestProjectData.SomeProjectNestedImportFile.TargetPath,
            [
                TestProjectData.SomeProjectNestedFile3.FilePath,
                TestProjectData.AnotherProjectNestedFile4.FilePath
            ]);

        var state = ProjectState.Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider);
        state.Documents = documents.ToImmutable();
        state.ImportsToRelatedDocuments = importsToRelatedDocuments.ToImmutable();

        // Act
        var newState = state.AddDocument(TestProjectData.AnotherProjectNestedImportFile, EmptyTextLoader.Instance);

        // Assert
        Assert.NotEqual(state.Version, newState.Version);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void ProjectState_WhenImportDocumentChangedTextLoader_CallsImportsChanged()
    {
        // Arrange
        var callCount = 0;

        var document1 = TestProjectData.SomeProjectFile1;
        var document2 = TestProjectData.SomeProjectFile2;
        var document3 = TestProjectData.SomeProjectNestedFile3;
        var document4 = TestProjectData.AnotherProjectNestedFile4;
        var document5 = TestProjectData.AnotherProjectNestedImportFile;

        var documents = ImmutableDictionary.CreateBuilder<string, DocumentState>(FilePathComparer.Instance);
        documents[document1.FilePath] = TestDocumentState.Create(document1, onImportsChange: () => callCount++);
        documents[document2.FilePath] = TestDocumentState.Create(document2, onImportsChange: () => callCount++);
        documents[document3.FilePath] = TestDocumentState.Create(document3, onImportsChange: () => callCount++);
        documents[document4.FilePath] = TestDocumentState.Create(document4, onImportsChange: () => callCount++);
        documents[document5.FilePath] = TestDocumentState.Create(document5, onImportsChange: () => callCount++);

        var importsToRelatedDocuments = ImmutableDictionary.CreateBuilder<string, ImmutableArray<string>>(FilePathComparer.Instance);
        importsToRelatedDocuments.Add(
            TestProjectData.SomeProjectImportFile.TargetPath,
            [
                TestProjectData.SomeProjectFile1.FilePath,
                TestProjectData.SomeProjectFile2.FilePath,
                TestProjectData.SomeProjectNestedFile3.FilePath,
                TestProjectData.AnotherProjectNestedFile4.FilePath,
                TestProjectData.AnotherProjectNestedImportFile.FilePath,
            ]);
        importsToRelatedDocuments.Add(
            TestProjectData.SomeProjectNestedImportFile.TargetPath,
            [
                TestProjectData.SomeProjectNestedFile3.FilePath,
                TestProjectData.AnotherProjectNestedFile4.FilePath
            ]);

        var state = ProjectState.Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider);
        state.Documents = documents.ToImmutable();
        state.ImportsToRelatedDocuments = importsToRelatedDocuments.ToImmutable();

        // Act
        var newState = state.UpdateDocumentText(document5, EmptyTextLoader.Instance);

        // Assert
        Assert.NotEqual(state.Version, newState.Version);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void ProjectState_WhenImportDocumentChangedSnapshot_CallsImportsChanged()
    {
        // Arrange
        var callCount = 0;

        var document1 = TestProjectData.SomeProjectFile1;
        var document2 = TestProjectData.SomeProjectFile2;
        var document3 = TestProjectData.SomeProjectNestedFile3;
        var document4 = TestProjectData.AnotherProjectNestedFile4;
        var document5 = TestProjectData.AnotherProjectNestedImportFile;

        var documents = ImmutableDictionary.CreateBuilder<string, DocumentState>(FilePathComparer.Instance);
        documents[document1.FilePath] = TestDocumentState.Create(document1, onImportsChange: () => callCount++);
        documents[document2.FilePath] = TestDocumentState.Create(document2, onImportsChange: () => callCount++);
        documents[document3.FilePath] = TestDocumentState.Create(document3, onImportsChange: () => callCount++);
        documents[document4.FilePath] = TestDocumentState.Create(document4, onImportsChange: () => callCount++);
        documents[document5.FilePath] = TestDocumentState.Create(document5, onImportsChange: () => callCount++);

        var importsToRelatedDocuments = ImmutableDictionary.CreateBuilder<string, ImmutableArray<string>>(FilePathComparer.Instance);
        importsToRelatedDocuments.Add(
            TestProjectData.SomeProjectImportFile.TargetPath,
            [
                TestProjectData.SomeProjectFile1.FilePath,
                TestProjectData.SomeProjectFile2.FilePath,
                TestProjectData.SomeProjectNestedFile3.FilePath,
                TestProjectData.AnotherProjectNestedFile4.FilePath,
                TestProjectData.AnotherProjectNestedImportFile.FilePath,
            ]);
        importsToRelatedDocuments.Add(
            TestProjectData.SomeProjectNestedImportFile.TargetPath,
            [
                TestProjectData.SomeProjectNestedFile3.FilePath,
                TestProjectData.AnotherProjectNestedFile4.FilePath
            ]);

        var state = ProjectState.Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider);
        state.Documents = documents.ToImmutable();
        state.ImportsToRelatedDocuments = importsToRelatedDocuments.ToImmutable();

        // Act
        var newState = state.UpdateDocumentText(document5, s_text, VersionStamp.Create());

        // Assert
        Assert.NotEqual(state.Version, newState.Version);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void ProjectState_WhenImportDocumentRemoved_CallsImportsChanged()
    {
        // Arrange
        var callCount = 0;

        var document1 = TestProjectData.SomeProjectFile1;
        var document2 = TestProjectData.SomeProjectFile2;
        var document3 = TestProjectData.SomeProjectNestedFile3;
        var document4 = TestProjectData.AnotherProjectNestedFile4;
        var document5 = TestProjectData.AnotherProjectNestedImportFile;

        var documents = ImmutableDictionary.CreateBuilder<string, DocumentState>(FilePathComparer.Instance);
        documents[document1.FilePath] = TestDocumentState.Create(document1, onImportsChange: () => callCount++);
        documents[document2.FilePath] = TestDocumentState.Create(document2, onImportsChange: () => callCount++);
        documents[document3.FilePath] = TestDocumentState.Create(document3, onImportsChange: () => callCount++);
        documents[document4.FilePath] = TestDocumentState.Create(document4, onImportsChange: () => callCount++);
        documents[document5.FilePath] = TestDocumentState.Create(document5, onImportsChange: () => callCount++);

        var importsToRelatedDocuments = ImmutableDictionary.CreateBuilder<string, ImmutableArray<string>>(FilePathComparer.Instance);
        importsToRelatedDocuments.Add(
            TestProjectData.SomeProjectImportFile.TargetPath,
            [
                TestProjectData.SomeProjectFile1.FilePath,
                TestProjectData.SomeProjectFile2.FilePath,
                TestProjectData.SomeProjectNestedFile3.FilePath,
                TestProjectData.AnotherProjectNestedFile4.FilePath,
                TestProjectData.AnotherProjectNestedImportFile.FilePath,
            ]);
        importsToRelatedDocuments.Add(
            TestProjectData.SomeProjectNestedImportFile.TargetPath,
            [
                TestProjectData.SomeProjectNestedFile3.FilePath,
                TestProjectData.AnotherProjectNestedFile4.FilePath
            ]);

        var state = ProjectState.Create(s_project, s_projectWorkspaceState, s_projectEngineFactoryProvider);
        state.Documents = documents.ToImmutable();
        state.ImportsToRelatedDocuments = importsToRelatedDocuments.ToImmutable();

        // Act
        var newState = state.RemoveDocument(document5);

        // Assert
        Assert.NotEqual(state.Version, newState.Version);
        Assert.Equal(2, callCount);
    }

    private class TestDocumentState : DocumentState
    {
        public static TestDocumentState Create(
            HostDocument hostDocument,
            TextLoader? loader = null,
            Action? onConfigurationChange = null,
            Action? onImportsChange = null,
            Action? onProjectWorkspaceStateChange = null)
        {
            return new TestDocumentState(
                hostDocument,
                loader,
                onConfigurationChange,
                onImportsChange,
                onProjectWorkspaceStateChange);
        }

        private readonly Action? _onConfigurationChange;
        private readonly Action? _onImportsChange;
        private readonly Action? _onProjectWorkspaceStateChange;

        private TestDocumentState(
            HostDocument hostDocument,
            TextLoader? textLoader,
            Action? onConfigurationChange,
            Action? onImportsChange,
            Action? onProjectWorkspaceStateChange)
            : base(hostDocument, textLoader)
        {
            _onConfigurationChange = onConfigurationChange;
            _onImportsChange = onImportsChange;
            _onProjectWorkspaceStateChange = onProjectWorkspaceStateChange;
        }

        public override DocumentState WithConfigurationChange()
        {
            _onConfigurationChange?.Invoke();
            return base.WithConfigurationChange();
        }

        public override DocumentState WithImportsChange()
        {
            _onImportsChange?.Invoke();
            return base.WithImportsChange();
        }

        public override DocumentState WithProjectWorkspaceStateChange()
        {
            _onProjectWorkspaceStateChange?.Invoke();
            return base.WithProjectWorkspaceStateChange();
        }
    }
}
