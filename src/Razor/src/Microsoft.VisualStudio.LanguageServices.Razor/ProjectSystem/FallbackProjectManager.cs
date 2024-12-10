﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Telemetry;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Razor.ProjectSystem;

/// <summary>
/// This class is responsible for maintaining project information for projects that don't
/// use the Razor or Web SDK, or otherwise don't get picked up by our CPS bits, but have
/// .razor or .cshtml files regardless.
/// </summary>
[Export(typeof(IFallbackProjectManager))]
[Export(typeof(FallbackProjectManager))]
internal sealed class FallbackProjectManager : IFallbackProjectManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IProjectSnapshotManager _projectManager;
    private readonly IWorkspaceProvider _workspaceProvider;
    private readonly ITelemetryReporter _telemetryReporter;

    // Tracks project keys that are known to be fallback projects.
    private ImmutableHashSet<ProjectKey> _fallbackProjects = [];

    [ImportingConstructor]
    public FallbackProjectManager(
        [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
        IProjectSnapshotManager projectManager,
        IWorkspaceProvider workspaceProvider,
        ITelemetryReporter telemetryReporter)
    {
        _serviceProvider = serviceProvider;
        _projectManager = projectManager;
        _workspaceProvider = workspaceProvider;
        _telemetryReporter = telemetryReporter;

        // Use PriorityChanged to ensure that project changes or removes update _fallbackProjects
        // before IProjectSnapshotManager.Changed listeners are notified.
        _projectManager.PriorityChanged += ProjectManager_Changed;
    }

    private void ProjectManager_Changed(object sender, ProjectChangeEventArgs e)
    {
        // If a project is changed, we know that this is no longer a fallback project because
        // one of two things has happened:
        //
        // 1. The project system has updated the project's configuration or root namespace.
        // 2. The project's ProjectWorkspaceState has been updated.
        //
        // In either of these two cases, we assume that something else is properly tracking the
        // project and no longer treat it as a fallback project.
        //
        // In addition, if a project is removed, we can stop tracking it as a fallback project.
        if (e.Kind is ProjectChangeKind.ProjectChanged or ProjectChangeKind.ProjectRemoved)
        {
            ImmutableInterlocked.Update(ref _fallbackProjects, set => set.Remove(e.ProjectKey));
        }
    }

    public bool IsFallbackProject(IProjectSnapshot project)
        => _fallbackProjects.Contains(project.Key);

    public void DynamicFileAdded(
        ProjectId projectId,
        ProjectKey razorProjectKey,
        string projectFilePath,
        string filePath,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_projectManager.TryGetLoadedProject(razorProjectKey, out var project))
            {
                if (IsFallbackProject(project))
                {
                    // If this is a fallback project, then Roslyn may not track documents in the project, so these dynamic file notifications
                    // are the only way to know about files in the project.
                    AddFallbackDocument(razorProjectKey, filePath, projectFilePath, cancellationToken);
                }
            }
            else
            {
                // We have been asked to provide dynamic file info, which means there is a .razor or .cshtml file in the project
                // but for some reason our project system doesn't know about the project. In these cases (often when people don't
                // use the Razor or Web SDK) we spin up a fallback experience for them
                AddFallbackProject(projectId, filePath, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _telemetryReporter.ReportFault(ex, "Error while trying to add fallback document to project");
        }
    }

    public void DynamicFileRemoved(
        ProjectId projectId,
        ProjectKey razorProjectKey,
        string projectFilePath,
        string filePath,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_projectManager.TryGetLoadedProject(razorProjectKey, out var project) &&
                IsFallbackProject(project))
            {
                // If this is a fallback project, then Roslyn may not track documents in the project, so these dynamic file notifications
                // are the only way to know about files in the project.
                RemoveFallbackDocument(projectId, filePath, projectFilePath, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _telemetryReporter.ReportFault(ex, "Error while trying to remove fallback document from project");
        }
    }

    private void AddFallbackProject(ProjectId projectId, string filePath, CancellationToken cancellationToken)
    {
        var project = TryFindProjectForProjectId(projectId);
        if (project?.FilePath is null)
        {
            return;
        }

        // If we can't retrieve intermediate output path, we can't create a ProjectKey.
        // So, we have to ignore this project.
        var intermediateOutputPath = Path.GetDirectoryName(project.CompilationOutputInfo.AssemblyPath);
        if (intermediateOutputPath is null)
        {
            return;
        }

        // We create this as a fallback project so that other parts of the system can reason about them - eg we don't do code
        // generation for closed files for documents in these projects. If these projects become "real", either because capabilities
        // change or simply a timing difference between Roslyn and our CPS components, the HostProject instance associated with
        // the project will be updated, and it will no longer be a fallback project.
        var hostProject = new HostProject(
            project.FilePath,
            intermediateOutputPath,
            FallbackRazorConfiguration.Latest,
            project.DefaultNamespace,
            project.Name);

        ImmutableInterlocked.Update(ref _fallbackProjects, set => set.Add(hostProject.Key));

        EnqueueProjectManagerUpdate(
            updater => updater.ProjectAdded(hostProject),
            cancellationToken);

        AddFallbackDocument(hostProject.Key, filePath, project.FilePath, cancellationToken);
    }

    private void AddFallbackDocument(ProjectKey projectKey, string filePath, string projectFilePath, CancellationToken cancellationToken)
    {
        if (!TryCreateHostDocument(filePath, projectFilePath, out var hostDocument))
        {
            return;
        }

        var textLoader = new FileTextLoader(filePath, defaultEncoding: null);

        EnqueueProjectManagerUpdate(
            updater => updater.DocumentAdded(projectKey, hostDocument, textLoader),
            cancellationToken);
    }

    private static bool TryCreateHostDocument(string filePath, string projectFilePath, [NotNullWhen(true)] out HostDocument? hostDocument)
    {
        // The compiler only supports paths that are relative to the project root, so filter our files
        // that don't match
        var projectPath = FilePathNormalizer.GetNormalizedDirectoryName(projectFilePath);
        var normalizedFilePath = FilePathNormalizer.Normalize(filePath);

        if (normalizedFilePath.StartsWith(projectPath, FilePathComparison.Instance))
        {
            var targetPath = filePath[projectPath.Length..];
            hostDocument = new(filePath, targetPath);
            return true;
        }

        hostDocument = null;
        return false;
    }

    private void RemoveFallbackDocument(ProjectId projectId, string filePath, string projectFilePath, CancellationToken cancellationToken)
    {
        var project = TryFindProjectForProjectId(projectId);
        if (project is null)
        {
            return;
        }

        var projectKey = project.ToProjectKey();

        if (!TryCreateHostDocument(filePath, projectFilePath, out var hostDocument))
        {
            return;
        }

        EnqueueProjectManagerUpdate(
            updater => updater.DocumentRemoved(projectKey, hostDocument),
            cancellationToken);
    }

    private void EnqueueProjectManagerUpdate(Action<ProjectSnapshotManager.Updater> action, CancellationToken cancellationToken)
    {
        _projectManager
            .UpdateAsync(
                static (updater, state) =>
                {
                    var (serviceProvider, action) = state;
                    RazorStartupInitializer.Initialize(serviceProvider);

                    action(updater);
                },
                state: (_serviceProvider, action),
                cancellationToken)
            .Forget();
    }

    private Project? TryFindProjectForProjectId(ProjectId projectId)
    {
        var workspace = _workspaceProvider.GetWorkspace();

        var project = workspace.CurrentSolution.GetProject(projectId);
        if (project is null ||
            project.Language != LanguageNames.CSharp)
        {
            return null;
        }

        return project;
    }
}
