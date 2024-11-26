// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#if !NET
using System.Collections.Generic;
#endif
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;
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
    public static SolutionState Empty { get; } = new(
        projectKeyToEntryMap: ImmutableDictionary<ProjectKey, ProjectEntry>.Empty,
        openDocumentSet: ImmutableHashSet.Create<string>(FilePathComparer.Instance),
        isSolutionClosing: false);

    private readonly ImmutableDictionary<ProjectKey, ProjectEntry> _projectKeyToEntryMap;
    private readonly ImmutableHashSet<string> _openDocumentSet;

    private ImmutableArray<IProjectSnapshot> _projects;
    private ImmutableArray<string> _openDocuments;

    public bool IsSolutionClosing { get; }

    private SolutionState(
        ImmutableDictionary<ProjectKey, ProjectEntry> projectKeyToEntryMap,
        ImmutableHashSet<string> openDocumentSet,
        bool isSolutionClosing)
    {
        _projectKeyToEntryMap = projectKeyToEntryMap;
        _openDocumentSet = openDocumentSet;
        IsSolutionClosing = isSolutionClosing;
    }

    public ImmutableArray<IProjectSnapshot> GetProjects()
    {
        if (_projects.IsDefault)
        {
            ImmutableInterlocked.InterlockedInitialize(ref _projects, GetProjectsCore(_projectKeyToEntryMap));
        }

        return _projects;

        static ImmutableArray<IProjectSnapshot> GetProjectsCore(ImmutableDictionary<ProjectKey, ProjectEntry> projectKeyToEntryMap)
        {
            using var builder = new PooledArrayBuilder<IProjectSnapshot>(projectKeyToEntryMap.Count);

            foreach (var (_, entry) in projectKeyToEntryMap)
            {
                builder.Add(entry.Snapshot);
            }

            return builder.DrainToImmutableOrderedBy(static x => x.Key);
        }
    }

    public ImmutableArray<string> GetOpenDocuments()
    {
        if (_openDocuments.IsDefault)
        {
            ImmutableInterlocked.InterlockedInitialize(ref _openDocuments, GetOpenDocumentsCore(_openDocumentSet));
        }

        return _openDocuments;

        static ImmutableArray<string> GetOpenDocumentsCore(ImmutableHashSet<string> openDocumentSet)
        {
            using var builder = new PooledArrayBuilder<string>(openDocumentSet.Count);

            foreach (var documentFilePath in openDocumentSet)
            {
                builder.Add(documentFilePath);
            }

            return builder.DrainToImmutableOrdered();
        }
    }

    public IProjectSnapshot GetLoadedProject(ProjectKey projectKey)
    {
        if (_projectKeyToEntryMap.TryGetValue(projectKey, out var entry))
        {
            return entry.Snapshot;
        }

        throw new InvalidOperationException($"No project snapshot exists with the key, '{projectKey}'");
    }

    public bool TryGetLoadedProject(ProjectKey projectKey, [NotNullWhen(true)] out IProjectSnapshot? project)
    {
        if (_projectKeyToEntryMap.TryGetValue(projectKey, out var entry))
        {
            project = entry.Snapshot;
            return true;
        }

        project = null;
        return false;
    }

    public ImmutableArray<ProjectKey> GetAllProjectKeys(string projectFilePath)
    {
        using var projects = new PooledArrayBuilder<ProjectKey>(capacity: _projectKeyToEntryMap.Count);

        foreach (var (projectKey, entry) in _projectKeyToEntryMap)
        {
            if (FilePathComparer.Instance.Equals(entry.State.HostProject.FilePath, projectFilePath))
            {
                projects.Add(projectKey);
            }
        }

        return projects.DrainToImmutable();
    }

    public bool IsDocumentOpen(string documentFilePath)
        => _openDocumentSet.Contains(documentFilePath);

    public SolutionState AddProject(
        HostProject hostProject,
        IProjectEngineFactoryProvider projectEngineFactoryProvider,
        LanguageServerFeatureOptions languageServerFeatureOptions)
    {
        // Don't compute new state if the solution is closing.
        if (IsSolutionClosing)
        {
            return this;
        }

        if (_projectKeyToEntryMap.ContainsKey(hostProject.Key))
        {
            // Project already exists.
            // TODO: Log warning?
            return this;
        }

        var projectState = ProjectState.Create(projectEngineFactoryProvider, languageServerFeatureOptions, hostProject, ProjectWorkspaceState.Default);

        return new(_projectKeyToEntryMap.Add(hostProject.Key, new(projectState)), _openDocumentSet, IsSolutionClosing);
    }

    public SolutionState RemoveProject(ProjectKey projectKey)
    {
        // Don't compute new state if the solution is closing.
        if (IsSolutionClosing)
        {
            return this;
        }

        if (!_projectKeyToEntryMap.ContainsKey(projectKey))
        {
            // Project does not exist.
            // TODO: Log warning?
            return this;
        }

        // TODO: Should we remove any documents contained within the project from the open document set?

        return new(_projectKeyToEntryMap.Remove(projectKey), _openDocumentSet, IsSolutionClosing);
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

    public SolutionState OpenDocument(ProjectKey projectKey, string documentFilePath, SourceText sourceText)
    {
        return UpdateProject(
            projectKey,
            projectStateUpdater: projectState => projectState.UpdateDocumentText(documentFilePath, sourceText),
            solutionStateUpdater: (solutionState, newProjectState) => new(
                projectKeyToEntryMap: solutionState._projectKeyToEntryMap.SetItem(projectKey, new(newProjectState)),
                openDocumentSet: _openDocumentSet.Add(documentFilePath),
                IsSolutionClosing));
    }

    public SolutionState CloseDocument(ProjectKey projectKey, string documentFilePath, TextLoader textLoader)
    {
        return UpdateProject(
            projectKey,
            projectStateUpdater: projectState => projectState.UpdateDocumentText(documentFilePath, textLoader),
            solutionStateUpdater: (solutionState, newProjectState) => new(
                projectKeyToEntryMap: solutionState._projectKeyToEntryMap.SetItem(projectKey, new(newProjectState)),
                openDocumentSet: _openDocumentSet.Remove(documentFilePath),
                IsSolutionClosing));
    }

    private SolutionState UpdateProject(
        ProjectKey projectKey,
        Func<ProjectState, ProjectState> projectStateUpdater,
        Func<SolutionState, ProjectState, SolutionState>? solutionStateUpdater = null)
    {
        // Don't compute new state if the solution is closing.
        if (IsSolutionClosing)
        {
            return this;
        }

        if (!_projectKeyToEntryMap.TryGetValue(projectKey, out var entry))
        {
            // Project does not exist.
            // TODO: Log warning?
            return this;
        }

        var newProjectState = projectStateUpdater(entry.State);
        if (ReferenceEquals(entry.State, newProjectState))
        {
            return this;
        }

        if (solutionStateUpdater is not null)
        {
            return solutionStateUpdater(this, newProjectState);
        }

        return new(_projectKeyToEntryMap.SetItem(projectKey, new(newProjectState)), _openDocumentSet, IsSolutionClosing);
    }

    public SolutionState UpdateIsSolutionClosing(bool value)
        => IsSolutionClosing == value
            ? this
            : new(_projectKeyToEntryMap, _openDocumentSet, value);
}
