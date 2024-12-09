// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.LiveShare;
using Microsoft.VisualStudio.Threading;
using IAsyncDisposable = Microsoft.VisualStudio.Threading.IAsyncDisposable;

namespace Microsoft.VisualStudio.Razor.LiveShare.Guest;

internal class ProjectSnapshotSynchronizationService(
    CollaborationSession sessionContext,
    IProjectSnapshotManagerProxy hostProjectManagerProxy,
    IProjectSnapshotManager projectManager,
    ILoggerFactory loggerFactory,
    JoinableTaskFactory jtf) : ICollaborationService, IAsyncDisposable, System.IAsyncDisposable
{
    private readonly JoinableTaskFactory _jtf = jtf;
    private readonly CollaborationSession _sessionContext = sessionContext;
    private readonly IProjectSnapshotManagerProxy _hostProjectManagerProxy = hostProjectManagerProxy;
    private readonly IProjectSnapshotManager _projectManager = projectManager;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<ProjectSnapshotSynchronizationService>();

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        // We wire the changed event up early because any changed events that fire will ensure we have the most
        // up-to-date state.
        _hostProjectManagerProxy.Changed += HostProxyProjectManager_Changed;

        var projectManagerState = await _hostProjectManagerProxy.GetProjectManagerStateAsync(cancellationToken);

        await InitializeGuestProjectManagerAsync(projectManagerState.ProjectHandles);
    }

    public async Task DisposeAsync()
    {
        _hostProjectManagerProxy.Changed -= HostProxyProjectManager_Changed;

        var projects = _projectManager.GetProjects();

        await _projectManager.UpdateAsync(
            static (updater, state) =>
            {
                var (projects, logger) = state;

                foreach (var project in projects)
                {
                    try
                    {
                        updater.ProjectRemoved(project.Key);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error encountered from project '{project.FilePath}':{Environment.NewLine}{ex}");
                    }
                }
            },
            state: (projects, _logger),
            CancellationToken.None);
    }

    ValueTask System.IAsyncDisposable.DisposeAsync()
    {
        return new ValueTask(DisposeAsync());
    }

    // Internal for testing
    internal async ValueTask UpdateGuestProjectManagerAsync(ProjectChangeEventProxyArgs args)
    {
        if (args.Kind == ProjectProxyChangeKind.ProjectAdded)
        {
            var newer = args.Newer.AssumeNotNull();

            var guestPath = ResolveGuestPath(newer.FilePath);
            var guestIntermediateOutputPath = ResolveGuestPath(newer.IntermediateOutputPath);
            var hostProject = new HostProject(guestPath, guestIntermediateOutputPath, newer.Configuration, newer.RootNamespace);

            await _projectManager.UpdateAsync(
                static (updater, state) =>
                {
                    updater.ProjectAdded(state.hostProject);

                    if (state.projectWorkspaceState != null)
                    {
                        updater.ProjectWorkspaceStateChanged(state.hostProject.Key, state.projectWorkspaceState);
                    }
                },
                state: (hostProject, projectWorkspaceState: newer.ProjectWorkspaceState),
                CancellationToken.None);
        }
        else if (args.Kind == ProjectProxyChangeKind.ProjectRemoved)
        {
            var older = args.Older.AssumeNotNull();

            var guestPath = ResolveGuestPath(older.FilePath);

            await _projectManager.UpdateAsync(
                static (updater, guestPath) =>
                {
                    var projectKeys = updater.GetAllProjectKeys(guestPath);
                    foreach (var projectKey in projectKeys)
                    {
                        updater.ProjectRemoved(projectKey);
                    }
                },
                state: guestPath,
                CancellationToken.None);
        }
        else if (args.Kind == ProjectProxyChangeKind.ProjectChanged)
        {
            var older = args.Older.AssumeNotNull();
            var newer = args.Newer.AssumeNotNull();

            if (older.Configuration != newer.Configuration)
            {
                var guestPath = ResolveGuestPath(newer.FilePath);
                var guestIntermediateOutputPath = ResolveGuestPath(newer.IntermediateOutputPath);

                var projectKey = new ProjectKey(guestIntermediateOutputPath);
                var newConfiguration = newer.Configuration;
                var newRootNamespace = newer.RootNamespace;

                await _projectManager.UpdateAsync(
                    static (updater, state) =>
                    {
                        updater.UpdateProjectConfiguration(state.projectKey, state.newConfiguration);
                        updater.UpdateRootNamespace(state.projectKey, state.newRootNamespace);
                    },
                    state: (projectKey, newConfiguration, newRootNamespace),
                    CancellationToken.None);
            }
            else if (!ReferenceEquals(older.ProjectWorkspaceState, newer.ProjectWorkspaceState) ||
                !older.ProjectWorkspaceState.Equals(newer.ProjectWorkspaceState))
            {
                var guestPath = ResolveGuestPath(args.Newer.FilePath);

                await _projectManager.UpdateAsync(
                    static (updater, state) =>
                    {
                        var projectKeys = updater.GetAllProjectKeys(state.guestPath);

                        foreach (var projectKey in projectKeys)
                        {
                            updater.ProjectWorkspaceStateChanged(projectKey, state.projectWorkspaceState);
                        }
                    },
                    state: (guestPath, projectWorkspaceState: args.Newer.ProjectWorkspaceState),
                    CancellationToken.None);
            }
        }
    }

    private async Task InitializeGuestProjectManagerAsync(IReadOnlyList<ProjectSnapshotHandleProxy> projectHandles)
    {
        foreach (var projectHandle in projectHandles)
        {
            var guestPath = ResolveGuestPath(projectHandle.FilePath);
            var guestIntermediateOutputPath = ResolveGuestPath(projectHandle.IntermediateOutputPath);
            var hostProject = new HostProject(guestPath, guestIntermediateOutputPath, projectHandle.Configuration, projectHandle.RootNamespace);
            await _projectManager.UpdateAsync(
                static (updater, state) =>
                {
                    updater.ProjectAdded(state.hostProject);

                    if (state.projectWorkspaceState is not null)
                    {
                        updater.ProjectWorkspaceStateChanged(state.hostProject.Key, state.projectWorkspaceState);
                    }
                },
                state: (hostProject, projectWorkspaceState: projectHandle.ProjectWorkspaceState),
                CancellationToken.None);
        }
    }

    private void HostProxyProjectManager_Changed(object sender, ProjectChangeEventProxyArgs args)
    {
        if (args is null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        _jtf.Run(async () =>
        {
            try
            {
                await UpdateGuestProjectManagerAsync(args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
        });
    }

    private string ResolveGuestPath(Uri filePath)
    {
        return _sessionContext.ConvertSharedUriToLocalPath(filePath);
    }
}
