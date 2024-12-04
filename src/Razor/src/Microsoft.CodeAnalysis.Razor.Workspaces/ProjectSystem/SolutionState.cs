// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

/// <summary>
///  Represents a Razor-specific view of the world. The only projects tracked within contain Razor
///  files.
/// </summary>
internal sealed partial class SolutionState
{
    private readonly IProjectEngineFactoryProvider _projectEngineFactoryProvider;
    private readonly LanguageServerFeatureOptions _languageServerFeatureOptions;

    private readonly ImmutableDictionary<ProjectKey, ProjectState> _projectKeyToStateMap;

    public bool IsSolutionClosing { get; }

    private SolutionState(
        IProjectEngineFactoryProvider projectEngineFactoryProvider,
        LanguageServerFeatureOptions languageServerFeatureOptions,
        ImmutableDictionary<ProjectKey, ProjectState> projectKeyToStateMap,
        bool isSolutionClosing)
    {
        _projectEngineFactoryProvider = projectEngineFactoryProvider;
        _languageServerFeatureOptions = languageServerFeatureOptions;
        _projectKeyToStateMap = projectKeyToStateMap;
        IsSolutionClosing = isSolutionClosing;
    }

    public static SolutionState Create(
        IProjectEngineFactoryProvider projectEngineFactoryProvider,
        LanguageServerFeatureOptions languageServerFeatureOptions)
        => new(
            projectEngineFactoryProvider,
            languageServerFeatureOptions,
            projectKeyToStateMap: ImmutableDictionary<ProjectKey, ProjectState>.Empty,
            isSolutionClosing: false);

    public ImmutableDictionary<ProjectKey, ProjectState> ProjectStates => _projectKeyToStateMap;

    public SolutionState AddProject(HostProject hostProject)
    {
        // Don't compute new state if the solution is closing.
        if (IsSolutionClosing)
        {
            return this;
        }

        if (_projectKeyToStateMap.ContainsKey(hostProject.Key))
        {
            // Project already exists.
            // TODO: Log warning?
            return this;
        }

        var projectState = ProjectState.Create(_projectEngineFactoryProvider, _languageServerFeatureOptions, hostProject, ProjectWorkspaceState.Default);

        return new(
            _projectEngineFactoryProvider,
            _languageServerFeatureOptions,
            _projectKeyToStateMap.Add(hostProject.Key, projectState),
            IsSolutionClosing);
    }

    public SolutionState RemoveProject(ProjectKey projectKey)
    {
        // Don't compute new state if the solution is closing.
        if (IsSolutionClosing)
        {
            return this;
        }

        if (!_projectKeyToStateMap.ContainsKey(projectKey))
        {
            // Project does not exist.
            // TODO: Log warning?
            return this;
        }

        // TODO: Should we remove any documents contained within the project from the open document set?

        return new(
            _projectEngineFactoryProvider,
            _languageServerFeatureOptions,
            _projectKeyToStateMap.Remove(projectKey),
            IsSolutionClosing);
    }

    public SolutionState UpdateProjectConfiguration(HostProject hostProject)
    {
        return UpdateProject(hostProject.Key, projectState => projectState.WithHostProject(hostProject));
    }

    public SolutionState UpdateProjectWorkspaceState(ProjectKey projectKey, ProjectWorkspaceState projectWorkspaceState)
    {
        return UpdateProject(projectKey, projectState => projectState.WithProjectWorkspaceState(projectWorkspaceState));
    }

    public SolutionState AddDocument(ProjectKey projectKey, HostDocument hostDocument, TextLoader textLoader)
    {
        return UpdateProject(projectKey, projectState => projectState.AddDocument(hostDocument, textLoader));
    }

    public SolutionState RemoveDocument(ProjectKey projectKey, string documentFilePath)
    {
        return UpdateProject(projectKey, projectState => projectState.RemoveDocument(documentFilePath));
    }

    public SolutionState UpdateDocumentText(ProjectKey projectKey, string documentFilePath, TextLoader textLoader)
    {
        return UpdateProject(projectKey, projectState => projectState.UpdateDocumentText(documentFilePath, textLoader));
    }

    public SolutionState UpdateDocumentText(ProjectKey projectKey, string documentFilePath, SourceText sourceText)
    {
        return UpdateProject(projectKey, projectState => projectState.UpdateDocumentText(documentFilePath, sourceText));
    }

    private SolutionState UpdateProject(
        ProjectKey projectKey,
        Func<ProjectState, ProjectState> projectStateUpdater)
    {
        // Don't compute new state if the solution is closing.
        if (IsSolutionClosing)
        {
            return this;
        }

        if (!_projectKeyToStateMap.TryGetValue(projectKey, out var oldState))
        {
            // Project does not exist.
            // TODO: Log warning?
            return this;
        }

        var newState = projectStateUpdater(oldState);
        if (ReferenceEquals(oldState, newState))
        {
            return this;
        }

        return new(
            _projectEngineFactoryProvider,
            _languageServerFeatureOptions,
            _projectKeyToStateMap.SetItem(projectKey, newState),
            IsSolutionClosing);
    }

    public SolutionState UpdateIsSolutionClosing(bool value)
        => IsSolutionClosing == value
            ? this
            : new(
                _projectEngineFactoryProvider,
                _languageServerFeatureOptions,
                _projectKeyToStateMap,
                value);
}
