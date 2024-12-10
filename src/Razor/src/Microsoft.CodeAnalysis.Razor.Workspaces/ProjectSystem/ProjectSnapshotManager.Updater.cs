﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal partial class ProjectSnapshotManager
{
    public readonly struct Updater(ProjectSnapshotManager instance)
    {
        public ImmutableArray<ProjectKey> GetAllProjectKeys(string projectFileName)
            => instance.GetAllProjectKeys(projectFileName);
        public ImmutableArray<IProjectSnapshot> GetProjects()
            => instance.GetProjects();

        public bool ContainsProject(ProjectKey projectKey)
            => instance.ContainsProject(projectKey);
        public bool TryGetProject(ProjectKey projectKey, [NotNullWhen(true)] out IProjectSnapshot? project)
            => instance.TryGetProject(projectKey, out project);

        public bool IsDocumentOpen(string documentFilePath)
            => instance.IsDocumentOpen(documentFilePath);
        public ImmutableArray<string> GetOpenDocuments()
            => instance.GetOpenDocuments();

        public void DocumentAdded(ProjectKey projectKey, HostDocument document, TextLoader textLoader)
            => instance.DocumentAdded(projectKey, document, textLoader);

        public void DocumentRemoved(ProjectKey projectKey, HostDocument document)
            => instance.DocumentRemoved(projectKey, document);

        public void DocumentChanged(ProjectKey projectKey, string documentFilePath, TextLoader textLoader)
            => instance.DocumentChanged(projectKey, documentFilePath, textLoader);

        public void DocumentChanged(ProjectKey projectKey, string documentFilePath, SourceText sourceText)
            => instance.DocumentChanged(projectKey, documentFilePath, sourceText);

        public void DocumentOpened(ProjectKey projectKey, string documentFilePath, SourceText sourceText)
            => instance.DocumentOpened(projectKey, documentFilePath, sourceText);

        public void DocumentClosed(ProjectKey projectKey, string documentFilePath, TextLoader textLoader)
            => instance.DocumentClosed(projectKey, documentFilePath, textLoader);

        public void ProjectAdded(HostProject project)
            => instance.ProjectAdded(project);

        public void ProjectRemoved(ProjectKey projectKey)
            => instance.ProjectRemoved(projectKey);

        public void UpdateProjectConfiguration(ProjectKey projectKey, RazorConfiguration configuration)
            => instance.UpdateProjectConfiguration(projectKey, configuration);

        public void UpdateRootNamespace(ProjectKey projectKey, string? rootNamespace)
            => instance.UpdateRootNamespace(projectKey, rootNamespace);

        public void ProjectWorkspaceStateChanged(ProjectKey projectKey, ProjectWorkspaceState projectWorkspaceState)
            => instance.ProjectWorkspaceStateChanged(projectKey, projectWorkspaceState);

        public void SolutionOpened()
            => instance.SolutionOpened();

        public void SolutionClosed()
            => instance.SolutionClosed();
    }
}
