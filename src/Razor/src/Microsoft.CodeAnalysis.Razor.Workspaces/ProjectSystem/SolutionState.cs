// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal sealed class SolutionState
{
    private readonly IProjectEngineFactoryProvider _projectEngineFactoryProvider;
    private readonly RazorCompilerOptions _compilerOptions;

    public ImmutableDictionary<ProjectKey, ProjectState> ProjectStates { get; }

    private SolutionState(IProjectEngineFactoryProvider projectEngineFactoryProvider, RazorCompilerOptions compilerOptions)
    {
        _projectEngineFactoryProvider = projectEngineFactoryProvider;
        _compilerOptions = compilerOptions;

        ProjectStates = ImmutableDictionary<ProjectKey, ProjectState>.Empty;
    }

    private SolutionState(SolutionState oldState, ImmutableDictionary<ProjectKey, ProjectState> projectStates)
    {
        _projectEngineFactoryProvider = oldState._projectEngineFactoryProvider;
        _compilerOptions = oldState._compilerOptions;

        ProjectStates = projectStates;
    }

    public static SolutionState Create(IProjectEngineFactoryProvider projectEngineFactoryProvider, RazorCompilerOptions compilerOptions)
        => new(projectEngineFactoryProvider, compilerOptions);

    public SolutionState AddProject(HostProject hostProject)
    {
        // If the project already exists, just return the same solution state.
        if (ProjectStates.ContainsKey(hostProject.Key))
        {
            return this;
        }

        var newProjectStates = ProjectStates.Add(
            hostProject.Key,
            ProjectState.Create(hostProject, _compilerOptions, _projectEngineFactoryProvider));

        return new(oldState: this, newProjectStates);
    }

    public SolutionState RemoveProject(ProjectKey projectKey)
    {
        var newProjectStates = ProjectStates.Remove(projectKey);

        // If the project wasn't present when we attempted to remove it, just return the same solution state.
        if (ProjectStates == newProjectStates)
        {
            return this;
        }

        return new(oldState: this, newProjectStates);
    }

    public SolutionState UpdateProjectConfiguration(HostProject hostProject)
    {
        return UpdateProjectState(hostProject.Key, state => state.WithHostProject(hostProject));
    }

    public SolutionState UpdateProjectWorkspaceState(ProjectKey projectKey, ProjectWorkspaceState projectWorkspaceState)
    {
        return UpdateProjectState(projectKey, state => state.WithProjectWorkspaceState(projectWorkspaceState));
    }

    public SolutionState AddDocument(ProjectKey projectKey, HostDocument hostDocument, SourceText text)
    {
        return UpdateProjectState(projectKey, state => state.AddDocument(hostDocument, text));
    }

    public SolutionState AddDocument(ProjectKey projectKey, HostDocument hostDocument, TextLoader textLoader)
    {
        return UpdateProjectState(projectKey, state => state.AddDocument(hostDocument, textLoader));
    }

    public SolutionState RemoveDocument(ProjectKey projectKey, string documentFilePath)
    {
        return UpdateProjectState(projectKey, state => state.RemoveDocument(documentFilePath));
    }

    private SolutionState UpdateProjectState(ProjectKey projectKey, Func<ProjectState, ProjectState> transformer)
    {
        // If the project doesn't exist, we're done.
        if (!ProjectStates.TryGetValue(projectKey, out var oldState))
        {
            return this;
        }

        var newState = transformer(oldState);

        // If updating returns the same project state, we're done.
        if (oldState == newState)
        {
            return this;
        }

        return new(this, ProjectStates.SetItem(projectKey, newState));
    }
}
