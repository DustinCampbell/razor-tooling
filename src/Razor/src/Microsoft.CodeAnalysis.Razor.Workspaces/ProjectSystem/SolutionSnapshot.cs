// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal sealed class SolutionSnapshot(SolutionState state) : ISolutionSnapshot
{
    private readonly SolutionState _state = state;

    private readonly object _gate = new();
    private readonly Dictionary<ProjectKey, ProjectSnapshot> _projectKeyToSnapshotMap = [];

    private ImmutableArray<IProjectSnapshot> _projects;

    public ImmutableArray<IProjectSnapshot> Projects
    {
        get
        {
            if (_projects.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref _projects, GetProjectsCore(_state.ProjectStates));
            }

            return _projects;

            ImmutableArray<IProjectSnapshot> GetProjectsCore(ImmutableDictionary<ProjectKey, ProjectState> projectKeyToEntryMap)
            {
                if (projectKeyToEntryMap.IsEmpty)
                {
                    return [];
                }

                lock (_gate)
                {
                    using var builder = new PooledArrayBuilder<IProjectSnapshot>(projectKeyToEntryMap.Count);

                    foreach (var (key, state) in projectKeyToEntryMap)
                    {
                        if (!_projectKeyToSnapshotMap.TryGetValue(key, out var result))
                        {
                            result = new(this, state);
                            _projectKeyToSnapshotMap.Add(key, result);
                        }

                        builder.Add(result);
                    }

                    return builder.DrainToImmutableOrderedBy(static x => x.Key);
                }
            }
        }
    }

    IEnumerable<IProjectSnapshot> ISolutionSnapshot.Projects
        => Projects;

    public ImmutableArray<ProjectKey> GetProjectKeysWithFilePath(string filePath)
    {
        using var projectKeys = new PooledArrayBuilder<ProjectKey>(capacity: _state.ProjectStates.Count);

        foreach (var (key, state) in _state.ProjectStates)
        {
            if (FilePathComparer.Instance.Equals(state.HostProject.FilePath, filePath))
            {
                projectKeys.Add(key);
            }
        }

        return projectKeys.DrainToImmutable();
    }

    public ProjectSnapshot? GetProject(ProjectKey projectKey)
        => TryGetLoadedProject(projectKey, out var project)
            ? project
            : null;

    public ProjectSnapshot GetRequiredProject(ProjectKey projectKey)
        => GetProject(projectKey).AssumeNotNull();

    public DocumentSnapshot? GetDocument(ProjectKey projectKey, string documentFilePath)
        => TryGetLoadedProject(projectKey, out var project)
            ? project.GetDocument(documentFilePath)
            : null;

    public DocumentSnapshot GetRequiredDocument(ProjectKey projectKey, string documentFilePath)
        => GetDocument(projectKey, documentFilePath).AssumeNotNull();

    public bool TryGetLoadedProject(ProjectKey projectKey, [NotNullWhen(true)] out ProjectSnapshot? project)
    {
        lock (_gate)
        {
            if (_projectKeyToSnapshotMap.TryGetValue(projectKey, out var snapshot))
            {
                project = snapshot;
                return true;
            }

            if (_state.ProjectStates.TryGetValue(projectKey, out var state))
            {
                snapshot = new ProjectSnapshot(this, state);
                _projectKeyToSnapshotMap.Add(projectKey, snapshot);
                project = snapshot;
                return true;
            }

            project = null;
            return false;
        }
    }

    bool ISolutionSnapshot.TryGetProject(ProjectKey projectKey, [NotNullWhen(true)] out IProjectSnapshot? project)
    {
        project = TryGetLoadedProject(projectKey, out var snapshot)
            ? snapshot
            : null;

        return project is not null;
    }

    bool ISolutionSnapshot.TryGetDocument(ProjectKey projectKey, string documentFilePath, [NotNullWhen(true)] out IDocumentSnapshot? document)
    {
        document = TryGetLoadedProject(projectKey, out var project)
            ? project.GetDocument(documentFilePath)
            : null;

        return document is not null;
    }

    public SolutionSnapshot AddProject(HostProject hostProject)
    {
        var newState = _state.AddProject(hostProject);

        if (ReferenceEquals(newState, _state))
        {
            return this;
        }

        return new SolutionSnapshot(newState);
    }

    public SolutionSnapshot RemoveProject(ProjectKey projectKey)
    {
        var newState = _state.RemoveProject(projectKey);

        if (ReferenceEquals(newState, _state))
        {
            return this;
        }

        return new SolutionSnapshot(newState);
    }

    public SolutionSnapshot UpdateProjectConfiguration(HostProject hostProject)
    {
        var newState = _state.UpdateProjectConfiguration(hostProject);

        if (ReferenceEquals(newState, _state))
        {
            return this;
        }

        return new SolutionSnapshot(newState);
    }

    public SolutionSnapshot UpdateProjectWorkspaceState(ProjectKey projectKey, ProjectWorkspaceState projectWorkspaceState)
    {
        var newState = _state.UpdateProjectWorkspaceState(projectKey, projectWorkspaceState);

        if (ReferenceEquals(newState, _state))
        {
            return this;
        }

        return new SolutionSnapshot(newState);
    }

    public SolutionSnapshot AddDocument(ProjectKey projectKey, HostDocument hostDocument, TextLoader textLoader)
    {
        var newState = _state.AddDocument(projectKey, hostDocument, textLoader);

        if (ReferenceEquals(newState, _state))
        {
            return this;
        }

        return new SolutionSnapshot(newState);
    }

    public SolutionSnapshot RemoveDocument(ProjectKey projectKey, string documentFilePath)
    {
        var newState = _state.RemoveDocument(projectKey, documentFilePath);

        if (ReferenceEquals(newState, _state))
        {
            return this;
        }

        return new SolutionSnapshot(newState);
    }

    public SolutionSnapshot UpdateDocumentText(ProjectKey projectKey, string documentFilePath, SourceText sourceText)
    {
        var newState = _state.UpdateDocumentText(projectKey, documentFilePath, sourceText);

        if (ReferenceEquals(newState, _state))
        {
            return this;
        }

        return new SolutionSnapshot(newState);
    }

    public SolutionSnapshot UpdateDocumentText(ProjectKey projectKey, string documentFilePath, TextLoader textLoader)
    {
        var newState = _state.UpdateDocumentText(projectKey, documentFilePath, textLoader);

        if (ReferenceEquals(newState, _state))
        {
            return this;
        }

        return new SolutionSnapshot(newState);
    }
}
