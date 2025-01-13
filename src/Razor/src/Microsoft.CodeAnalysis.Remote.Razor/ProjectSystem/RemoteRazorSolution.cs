// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

internal sealed class RemoteRazorSolution(Solution solution, RemoteSnapshotManager snapshotManager) : ISolutionQueryOperations
{
    public Solution UnderlyingSolution { get; } = solution;
    public RemoteSnapshotManager SnapshotManager { get; } = snapshotManager;

    private readonly Dictionary<Project, RemoteRazorProject> _projectMap = [];

    public bool TryGetProject(ProjectId projectId, [NotNullWhen(true)] out RemoteRazorProject? project)
    {
        if (UnderlyingSolution.GetProject(projectId) is not { } underlyingProject)
        {
            project = null;
            return false;
        }

        project = GetProject(underlyingProject);
        return true;
    }

    public RemoteRazorProject GetProject(ProjectId projectId)
    {
        var project = UnderlyingSolution.GetRequiredProject(projectId);
        return GetProject(project);
    }

    public RemoteRazorProject GetProject(Project project)
    {
        if (project.Solution != UnderlyingSolution)
        {
            throw new ArgumentException(SR.Project_does_not_belong_to_this_solution, nameof(project));
        }

        if (!project.ContainsRazorDocuments())
        {
            throw new ArgumentException(SR.Project_does_not_contain_any_Razor_documents, nameof(project));
        }

        return GetProjectCore(project);
    }

    private RemoteRazorProject GetProjectCore(Project project)
    {
        lock (_projectMap)
        {
            if (!_projectMap.TryGetValue(project, out var snapshot))
            {
                snapshot = new RemoteRazorProject(project, this);
                _projectMap.Add(project, snapshot);
            }

            return snapshot;
        }
    }

    public IEnumerable<IRazorProject> GetProjects()
        => UnderlyingSolution.Projects
            .Where(static p => p.ContainsRazorDocuments())
            .Select(GetProjectCore);

    public ImmutableArray<IRazorProject> GetProjectsContainingDocument(string documentFilePath)
    {
        if (!documentFilePath.IsRazorFilePath())
        {
            throw new ArgumentException(SR.Format0_is_not_a_Razor_file_path(documentFilePath), nameof(documentFilePath));
        }

        var documentIds = UnderlyingSolution.GetDocumentIdsWithFilePath(documentFilePath);

        if (documentIds.IsEmpty)
        {
            return [];
        }

        using var results = new PooledArrayBuilder<IRazorProject>(capacity: documentIds.Length);
        using var _ = HashSetPool<ProjectId>.GetPooledObject(out var projectIdSet);

        foreach (var documentId in documentIds)
        {
            var projectId = documentId.ProjectId;

            // We use a set to ensure that we only ever return the same project once.
            if (projectIdSet.Add(projectId))
            {
                // Since documentFilePath was proven to be a Razor file path, we know that
                // the projects will contain Razor documents and won't throw here.
                var project = UnderlyingSolution.GetRequiredProject(projectId);
                results.Add(GetProjectCore(project));
            }
        }

        return results.DrainToImmutable();
    }

    public bool TryGetDocument(Uri razorDocumentUri, [NotNullWhen(true)] out RemoteRazorDocument? document)
    {
        var documentId = UnderlyingSolution.GetDocumentIdsWithUri(razorDocumentUri).FirstOrDefault();

        if (documentId is null)
        {
            document = null;
            return false;
        }

        return TryGetDocument(documentId, out document);
    }

    public bool TryGetDocument(DocumentId documentId, [NotNullWhen(true)] out RemoteRazorDocument? document)
    {
        if (UnderlyingSolution.GetAdditionalDocument(documentId) is not { } textDocument)
        {
            document = null;
            return false;
        }

        document = GetDocument(textDocument);
        return true;
    }

    public RemoteRazorDocument GetDocument(DocumentId documentId)
    {
        var textDocument = UnderlyingSolution.GetRequiredAdditionalDocument(documentId);
        return GetProject(textDocument.Project).GetDocument(textDocument);
    }

    private RemoteRazorDocument GetDocument(TextDocument textDocument)
    {
        return GetProject(textDocument.Project).GetDocument(textDocument);
    }
}
