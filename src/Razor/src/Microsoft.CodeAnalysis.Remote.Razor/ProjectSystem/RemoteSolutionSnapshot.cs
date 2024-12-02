// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

internal sealed class RemoteSolutionSnapshot(Solution solution, RemoteSnapshotManager snapshotManager) : ISolutionSnapshot, ISolutionQueryOperations
{
    public RemoteSnapshotManager SnapshotManager { get; } = snapshotManager;

    private readonly Solution _solution = solution;

    private readonly object _gate = new();
    private readonly Dictionary<Project, RemoteProjectSnapshot> _projectToSnapshotMap = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<ProjectKey, RemoteProjectSnapshot> _projectKeyToSnapshotMap = [];

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

    public bool TryGetProject(ProjectKey projectKey, [NotNullWhen(true)] out RemoteProjectSnapshot? project)
    {
        var workspaceProject = _solution.Projects.FirstOrDefault(p => projectKey.Matches(p));

        project = workspaceProject is not null
            ? GetProject(workspaceProject)
            : null;

        return project is not null;
    }

    bool ISolutionSnapshot.TryGetProject(ProjectKey projectKey, [NotNullWhen(true)] out IProjectSnapshot? project)
    {
        project = TryGetProject(projectKey, out var snapshot)
            ? snapshot
            : null;

        return project is not null;
    }

    private RemoteProjectSnapshot GetProjectCore(Project project)
    {
        lock (_gate)
        {
            return GetProjectCore_NoLock(project);
        }
    }

    private RemoteProjectSnapshot GetProjectCore_NoLock(Project project)
    {
        if (!_projectToSnapshotMap.TryGetValue(project, out var snapshot))
        {
            snapshot = AddSnapshot_NoLock(project);
        }

        return snapshot;
    }

    private RemoteProjectSnapshot AddSnapshot_NoLock(Project project)
    {
        var projectKey = project.ToProjectKey();

        var snapshot = new RemoteProjectSnapshot(project, projectKey, solutionSnapshot: this);
        _projectToSnapshotMap.Add(project, snapshot);
        _projectKeyToSnapshotMap.Add(projectKey, snapshot);

        return snapshot;
    }

    public bool TryGetDocument(ProjectKey projectKey, string documentFilePath, [NotNullWhen(true)] out RemoteDocumentSnapshot? document)
    {
        document = TryGetProject(projectKey, out var project)
            ? project.GetDocument(documentFilePath)
            : null;

        return document is not null;
    }

    bool ISolutionSnapshot.TryGetDocument(ProjectKey projectKey, string documentFilePath, [NotNullWhen(true)] out IDocumentSnapshot? document)
    {
        document = TryGetDocument(projectKey, documentFilePath, out var snapshot)
            ? snapshot
            : null;

        return document is not null;
    }

    public IEnumerable<IProjectSnapshot> Projects
        => _solution.Projects
            .Where(static p => p.ContainsRazorDocuments())
            .Select(GetProjectCore);

    IEnumerable<IProjectSnapshot> ISolutionQueryOperations.GetProjects()
        => Projects;

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
