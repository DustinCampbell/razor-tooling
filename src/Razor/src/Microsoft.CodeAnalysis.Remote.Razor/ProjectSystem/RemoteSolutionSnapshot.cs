// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

internal sealed class RemoteSolutionSnapshot(Solution solution, RemoteSnapshotManager snapshotManager) : ISolutionSnapshot, ISolutionQueryOperations
{
    public RemoteSnapshotManager SnapshotManager { get; } = snapshotManager;

    private readonly Solution _solution = solution;
    private readonly Dictionary<Project, RemoteProjectSnapshot> _projectMap = [];

    public IEnumerable<IProjectSnapshot> Projects => GetProjects();

    public bool ContainsProject(ProjectKey projectKey)
    {
        foreach (var roslynProject in _solution.Projects)
        {
            if (projectKey.Matches(roslynProject))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGetProject(ProjectKey projectKey, [NotNullWhen(true)] out RemoteProjectSnapshot? project)
    {
        foreach (var roslynProject in _solution.Projects)
        {
            if (projectKey.Matches(roslynProject))
            {
                project = GetProjectCore(roslynProject);
                return true;
            }
        }

        project = null;
        return false;
    }

    bool ISolutionSnapshot.TryGetProject(ProjectKey projectKey, [NotNullWhen(true)] out IProjectSnapshot? project)
    {
        if (TryGetProject(projectKey, out var result))
        {
            project = result;
            return true;
        }

        project = null;
        return false;
    }

    public ImmutableArray<ProjectKey> GetProjectKeysWithFilePath(string filePath)
    {
        using var result = new PooledArrayBuilder<ProjectKey>();

        foreach (var roslynProject in _solution.Projects)
        {
            if (FilePathComparer.Instance.Equals(roslynProject.FilePath, filePath))
            {
                var project = GetProject(roslynProject);
                result.Add(project.Key);
            }
        }

        return result.DrainToImmutable();
    }

    public RemoteProjectSnapshot GetProject(ProjectId projectId)
    {
        var project = _solution.GetRequiredProject(projectId);
        return GetProject(project);
    }

    public RemoteProjectSnapshot GetProject(Project project)
    {
        if (project.Solution != _solution)
        {
            throw new ArgumentException(SR.Project_does_not_belong_to_this_solution, nameof(project));
        }

        if (!project.ContainsRazorDocuments())
        {
            throw new ArgumentException(SR.Project_does_not_contain_any_Razor_documents, nameof(project));
        }

        return GetProjectCore(project);
    }

    private RemoteProjectSnapshot GetProjectCore(Project project)
    {
        lock (_projectMap)
        {
            if (!_projectMap.TryGetValue(project, out var snapshot))
            {
                snapshot = new RemoteProjectSnapshot(project, this);
                _projectMap.Add(project, snapshot);
            }

            return snapshot;
        }
    }

    public IEnumerable<IProjectSnapshot> GetProjects()
        => _solution.Projects
            .Where(static p => p.ContainsRazorDocuments())
            .Select(GetProjectCore);

    public ImmutableArray<IProjectSnapshot> GetProjectsContainingDocument(string documentFilePath)
    {
        if (!documentFilePath.IsRazorFilePath())
        {
            throw new ArgumentException(SR.Format0_is_not_a_Razor_file_path(documentFilePath), nameof(documentFilePath));
        }

        var documentIds = _solution.GetDocumentIdsWithFilePath(documentFilePath);

        if (documentIds.IsEmpty)
        {
            return [];
        }

        using var results = new PooledArrayBuilder<IProjectSnapshot>(capacity: documentIds.Length);
        using var _ = HashSetPool<ProjectId>.GetPooledObject(out var projectIdSet);

        foreach (var documentId in documentIds)
        {
            var projectId = documentId.ProjectId;

            // We use a set to ensure that we only ever return the same project once.
            if (projectIdSet.Add(projectId))
            {
                // Since documentFilePath was proven to be a Razor file path, we know that
                // the projects will contain Razor documents and won't throw here.
                var project = _solution.GetRequiredProject(projectId);
                results.Add(GetProjectCore(project));
            }
        }

        return results.DrainToImmutable();
    }
}
