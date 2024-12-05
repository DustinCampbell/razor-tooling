﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

// The implementation of project snapshot manager abstracts the host's underlying project system (HostProject),
// to provide a immutable view of the underlying project systems.
//
// The HostProject support all of the configuration that the Razor SDK exposes via the project system
// (language version, extensions, named configuration).
//
// The implementation will create a ProjectSnapshot for each HostProject.
internal partial class ProjectSnapshotManager : IProjectSnapshotManager, IDisposable
{
    private readonly IProjectEngineFactoryProvider _projectEngineFactoryProvider;
    private readonly LanguageServerFeatureOptions _languageServerFeatureOptions;
    private readonly Dispatcher _dispatcher;
    private readonly bool _initialized;

    public event EventHandler<ProjectChangeEventArgs>? PriorityChanged;
    public event EventHandler<ProjectChangeEventArgs>? Changed;

    private readonly ReaderWriterLockSlim _readerWriterLock = new(LockRecursionPolicy.NoRecursion);

    #region protected by lock

    /// <summary>
    /// A map of <see cref="ProjectKey"/> to <see cref="Entry"/>, which wraps a <see cref="ProjectState"/>
    /// and lazily creates a <see cref="ProjectSnapshot"/>.
    /// </summary>
    private readonly Dictionary<ProjectKey, Entry> _projectMap = [];

    /// <summary>
    /// The set of open documents.
    /// </summary>
    private readonly HashSet<string> _openDocumentSet = new(FilePathComparer.Instance);

    /// <summary>
    /// Determines whether or not the solution is closing.
    /// </summary>
    private bool _isSolutionClosing;

    #endregion

    // We have a queue for changes because if one change results in another change aka, add -> open
    // we want to make sure the "add" finishes running first before "open" is notified.
    private readonly Queue<ProjectChangeEventArgs> _notificationQueue = new();

    /// <summary>
    /// Constructs an instance of <see cref="ProjectSnapshotManager"/>.
    /// </summary>
    /// <param name="projectEngineFactoryProvider">The <see cref="IProjectEngineFactoryProvider"/> to
    /// use when creating <see cref="ProjectState"/>.</param>
    /// <param name="languageServerFeatureOptions">The options that were used to start the language server</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use.</param>
    /// <param name="initializer">An optional callback to set up the initial set of projects and documents.
    /// Note that this is called during construction, so it does not run on the dispatcher and notifications
    /// will not be sent.</param>
    public ProjectSnapshotManager(
        IProjectEngineFactoryProvider projectEngineFactoryProvider,
        LanguageServerFeatureOptions languageServerFeatureOptions,
        ILoggerFactory loggerFactory,
        Action<Updater>? initializer = null)
    {
        _projectEngineFactoryProvider = projectEngineFactoryProvider;
        _languageServerFeatureOptions = languageServerFeatureOptions;
        _dispatcher = new(loggerFactory);

        initializer?.Invoke(new(this));

        _initialized = true;
    }

    public void Dispose()
    {
        _dispatcher.Dispose();
        _readerWriterLock.Dispose();
    }

    public bool IsSolutionClosing
    {
        get
        {
            using (_readerWriterLock.DisposableRead())
            {
                return _isSolutionClosing;
            }
        }
    }

    public ImmutableArray<IProjectSnapshot> GetProjects()
    {
        using (_readerWriterLock.DisposableRead())
        {
            using var builder = new PooledArrayBuilder<IProjectSnapshot>(_projectMap.Count);

            foreach (var (_, entry) in _projectMap)
            {
                builder.Add(entry.Snapshot);
            }

            return builder.DrainToImmutable();
        }
    }

    public ImmutableArray<string> GetOpenDocuments()
    {
        using (_readerWriterLock.DisposableRead())
        {
            return [.. _openDocumentSet];
        }
    }

    public IProjectSnapshot GetLoadedProject(ProjectKey projectKey)
    {
        using (_readerWriterLock.DisposableRead())
        {
            if (_projectMap.TryGetValue(projectKey, out var entry))
            {
                return entry.Snapshot;
            }
        }

        throw new InvalidOperationException($"No project snapshot exists with the key, '{projectKey}'");
    }

    public bool TryGetLoadedProject(ProjectKey projectKey, [NotNullWhen(true)] out IProjectSnapshot? project)
    {
        using (_readerWriterLock.DisposableRead())
        {
            if (_projectMap.TryGetValue(projectKey, out var entry))
            {
                project = entry.Snapshot;
                return true;
            }
        }

        project = null;
        return false;
    }

    public ImmutableArray<ProjectKey> GetAllProjectKeys(string projectFileName)
    {
        using (_readerWriterLock.DisposableRead())
        {
            using var projects = new PooledArrayBuilder<ProjectKey>(capacity: _projectMap.Count);

            foreach (var (key, entry) in _projectMap)
            {
                if (FilePathComparer.Instance.Equals(entry.State.HostProject.FilePath, projectFileName))
                {
                    projects.Add(key);
                }
            }

            return projects.DrainToImmutable();
        }
    }

    public bool IsDocumentOpen(string documentFilePath)
    {
        using (_readerWriterLock.DisposableRead())
        {
            return _openDocumentSet.Contains(documentFilePath);
        }
    }

    private void DocumentAdded(ProjectKey projectKey, HostDocument hostDocument, TextLoader textLoader)
    {
        if (_initialized)
        {
            _dispatcher.AssertRunningOnDispatcher();
        }

        if (TryUpdateProject(
            projectKey,
            projectStateUpdater: state => state.AddDocument(hostDocument, textLoader),
            out var oldSnapshot,
            out var newSnapshot))
        {
            NotifyListeners(ProjectChangeEventArgs.DocumentAdded(oldSnapshot, newSnapshot, hostDocument.FilePath, IsSolutionClosing));
        }
    }

    private void DocumentRemoved(ProjectKey projectKey, HostDocument hostDocument)
    {
        if (_initialized)
        {
            _dispatcher.AssertRunningOnDispatcher();
        }

        if (TryUpdateProject(
            projectKey,
            projectStateUpdater: state => state.RemoveDocument(hostDocument.FilePath),
            out var oldSnapshot,
            out var newSnapshot))
        {
            NotifyListeners(ProjectChangeEventArgs.DocumentRemoved(oldSnapshot, newSnapshot, hostDocument.FilePath, IsSolutionClosing));
        }
    }

    private void DocumentOpened(ProjectKey projectKey, string documentFilePath, SourceText sourceText)
    {
        if (_initialized)
        {
            _dispatcher.AssertRunningOnDispatcher();
        }

        if (TryUpdateProject(
            projectKey,
            projectStateUpdater: state => state.UpdateDocumentText(documentFilePath, sourceText),
            managerStateUpdater: () => _openDocumentSet.Add(documentFilePath),
            out var oldSnapshot,
            out var newSnapshot))
        {
            NotifyListeners(ProjectChangeEventArgs.DocumentChanged(oldSnapshot, newSnapshot, documentFilePath, IsSolutionClosing));
        }
    }

    private void DocumentClosed(ProjectKey projectKey, string documentFilePath, TextLoader textLoader)
    {
        if (_initialized)
        {
            _dispatcher.AssertRunningOnDispatcher();
        }

        if (TryUpdateProject(
            projectKey,
            projectStateUpdater: state => state.UpdateDocumentText(documentFilePath, textLoader),
            managerStateUpdater: () => _openDocumentSet.Remove(documentFilePath),
            out var oldSnapshot,
            out var newSnapshot))
        {
            NotifyListeners(ProjectChangeEventArgs.DocumentChanged(oldSnapshot, newSnapshot, documentFilePath, IsSolutionClosing));
        }
    }

    private void DocumentChanged(ProjectKey projectKey, string documentFilePath, SourceText sourceText)
    {
        if (_initialized)
        {
            _dispatcher.AssertRunningOnDispatcher();
        }

        if (TryUpdateProject(
            projectKey,
            projectStateUpdater: state => state.UpdateDocumentText(documentFilePath, sourceText),
            out var oldSnapshot,
            out var newSnapshot))
        {
            NotifyListeners(ProjectChangeEventArgs.DocumentChanged(oldSnapshot, newSnapshot, documentFilePath, IsSolutionClosing));
        }
    }

    private void DocumentChanged(ProjectKey projectKey, string documentFilePath, TextLoader textLoader)
    {
        if (_initialized)
        {
            _dispatcher.AssertRunningOnDispatcher();
        }

        if (TryUpdateProject(
            projectKey,
            projectStateUpdater: state => state.UpdateDocumentText(documentFilePath, textLoader),
            out var oldSnapshot,
            out var newSnapshot))
        {
            NotifyListeners(ProjectChangeEventArgs.DocumentChanged(oldSnapshot, newSnapshot, documentFilePath, IsSolutionClosing));
        }
    }

    private void ProjectAdded(HostProject hostProject)
    {
        if (_initialized)
        {
            _dispatcher.AssertRunningOnDispatcher();
        }

        if (TryAddProject(hostProject, out var newSnapshot))
        {
            NotifyListeners(ProjectChangeEventArgs.ProjectAdded(newSnapshot, IsSolutionClosing));
        }
    }

    private void ProjectRemoved(ProjectKey projectKey)
    {
        if (_initialized)
        {
            _dispatcher.AssertRunningOnDispatcher();
        }

        if (TryRemoveProject(projectKey, out var oldSnapshot))
        {
            NotifyListeners(ProjectChangeEventArgs.ProjectRemoved(oldSnapshot, IsSolutionClosing));
        }
    }

    private void ProjectWorkspaceStateChanged(ProjectKey projectKey, ProjectWorkspaceState projectWorkspaceState)
    {
        if (_initialized)
        {
            _dispatcher.AssertRunningOnDispatcher();
        }

        if (TryUpdateProject(
            projectKey,
            projectStateUpdater: state => state.WithProjectWorkspaceState(projectWorkspaceState),
            out var oldSnapshot,
            out var newSnapshot))
        {
            NotifyListeners(ProjectChangeEventArgs.ProjectChanged(oldSnapshot, newSnapshot, IsSolutionClosing));
        }
    }

    private void ProjectConfigurationChanged(HostProject hostProject)
    {
        if (_initialized)
        {
            _dispatcher.AssertRunningOnDispatcher();
        }

        if (TryUpdateProject(
            hostProject.Key,
            projectStateUpdater: state => state.WithHostProject(hostProject),
            out var oldSnapshot,
            out var newSnapshot))
        {
            NotifyListeners(ProjectChangeEventArgs.ProjectChanged(oldSnapshot, newSnapshot, IsSolutionClosing));
        }
    }

    private void SolutionOpened()
    {
        if (_initialized)
        {
            _dispatcher.AssertRunningOnDispatcher();
        }

        using (_readerWriterLock.DisposableWrite())
        {
            _isSolutionClosing = false;
        }
    }

    private void SolutionClosed()
    {
        if (_initialized)
        {
            _dispatcher.AssertRunningOnDispatcher();
        }

        using (_readerWriterLock.DisposableWrite())
        {
            _isSolutionClosing = true;
        }
    }

    private void NotifyListeners(ProjectChangeEventArgs args)
    {
        if (!_initialized)
        {
            return;
        }

        _dispatcher.AssertRunningOnDispatcher();

        _notificationQueue.Enqueue(args);

        if (_notificationQueue.Count == 1)
        {
            // Only one notification, go ahead and start notifying. In the situation where Count > 1
            // it means an event was triggered as a response to another event. To ensure order we won't
            // immediately re-invoke Changed here, we'll wait for the stack to unwind to notify others.
            // This process still happens synchronously it just ensures that events happen in the correct
            // order. For instance, let's take the situation where a document is added to a project.
            // That document will be added and then opened. However, if the result of "adding" causes an
            // "open" to trigger we want to ensure that "add" finishes prior to "open" being notified.

            // Start unwinding the notification queue
            do
            {
                // Don't dequeue yet, we want the notification to sit in the queue until we've finished
                // notifying to ensure other calls to NotifyListeners know there's a currently running event loop.
                var currentArgs = _notificationQueue.Peek();
                PriorityChanged?.Invoke(this, currentArgs);
                Changed?.Invoke(this, currentArgs);

                _notificationQueue.Dequeue();
            }
            while (_notificationQueue.Count > 0);
        }
    }

    private bool TryAddProject(HostProject hostProject, [NotNullWhen(true)] out IProjectSnapshot? newSnapshot)
    {
        using var upgradeableLock = _readerWriterLock.DisposableUpgradeableRead();

        // If the project already exists, we can't add it again, so return false.
        if (_projectMap.ContainsKey(hostProject.Key))
        {
            newSnapshot = null;
            return false;
        }

        // ... otherwise, add the project and return true.

        var newState = ProjectState.Create(
            _projectEngineFactoryProvider,
            _languageServerFeatureOptions,
            hostProject,
            ProjectWorkspaceState.Default);

        var newEntry = new Entry(newState);

        upgradeableLock.EnterWrite();

        _projectMap.Add(hostProject.Key, newEntry);

        newSnapshot = newEntry.Snapshot;
        return true;
    }

    private bool TryRemoveProject(
        ProjectKey projectKey,
        [NotNullWhen(true)] out IProjectSnapshot? oldSnapshot)
    {
        using var upgradeableLock = _readerWriterLock.DisposableUpgradeableRead();

        if (!_projectMap.TryGetValue(projectKey, out var entry))
        {
            oldSnapshot = null;
            return false;
        }

        // If the solution is closing we don't need to bother computing new state
        if (!_isSolutionClosing)
        {
            upgradeableLock.EnterWrite();

            _projectMap.Remove(projectKey);
        }

        oldSnapshot = entry.Snapshot;
        return true;
    }

    private bool TryUpdateProject(
        ProjectKey projectKey,
        Func<ProjectState, ProjectState> projectStateUpdater,
        [NotNullWhen(true)] out IProjectSnapshot? oldSnapshot,
        [NotNullWhen(true)] out IProjectSnapshot? newSnapshot)
        => TryUpdateProject(projectKey, projectStateUpdater, managerStateUpdater: null, out oldSnapshot, out newSnapshot);

    private bool TryUpdateProject(
        ProjectKey projectKey,
        Func<ProjectState, ProjectState> projectStateUpdater,
        Action? managerStateUpdater,
        [NotNullWhen(true)] out IProjectSnapshot? oldSnapshot,
        [NotNullWhen(true)] out IProjectSnapshot? newSnapshot)
    {
        using var upgradeableLock = _readerWriterLock.DisposableUpgradeableRead();

        if (!_projectMap.TryGetValue(projectKey, out var entry))
        {
            oldSnapshot = newSnapshot = null;
            return false;
        }

        // if the solution is closing we don't need to bother computing new state
        if (_isSolutionClosing)
        {
            oldSnapshot = newSnapshot = entry.Snapshot;
            return true;
        }

        // ... otherwise, compute a new entry and update if it's changed from the old state.
        var newState = projectStateUpdater(entry.State);

        if (ReferenceEquals(newState, entry.State))
        {
            oldSnapshot = newSnapshot = null;
            return false;
        }

        upgradeableLock.EnterWrite();

        var newEntry = new Entry(newState);
        _projectMap[projectKey] = newEntry;

        managerStateUpdater?.Invoke();

        oldSnapshot = entry.Snapshot;
        newSnapshot = newEntry.Snapshot;

        return true;
    }

    public Task UpdateAsync(Action<Updater> updater, CancellationToken cancellationToken)
    {
        return _dispatcher.RunAsync(
            static x => x.updater(new(x.instance)),
            (updater, instance: this),
            cancellationToken);
    }

    public Task UpdateAsync<TState>(Action<Updater, TState> updater, TState state, CancellationToken cancellationToken)
    {
        return _dispatcher.RunAsync(
            static x => x.updater(new(x.instance), x.state),
            (updater, state, instance: this),
            cancellationToken);
    }

    public Task<TResult> UpdateAsync<TResult>(Func<Updater, TResult> updater, CancellationToken cancellationToken)
    {
        return _dispatcher.RunAsync(
            static x => x.updater(new(x.instance)),
            (updater, instance: this),
            cancellationToken);
    }

    public Task<TResult> UpdateAsync<TState, TResult>(Func<Updater, TState, TResult> updater, TState state, CancellationToken cancellationToken)
    {
        return _dispatcher.RunAsync(
            static x => x.updater(new(x.instance), x.state),
            (updater, state, instance: this),
            cancellationToken);
    }

    public Task UpdateAsync(Func<Updater, Task> updater, CancellationToken cancellationToken)
    {
        return _dispatcher.RunAsync(
            static x => x.updater(new(x.instance)),
            (updater, instance: this),
            cancellationToken).Unwrap();
    }

    public Task UpdateAsync<TState>(Func<Updater, TState, Task> updater, TState state, CancellationToken cancellationToken)
    {
        return _dispatcher.RunAsync(
            static x => x.updater(new(x.instance), x.state),
            (updater, state, instance: this),
            cancellationToken).Unwrap();
    }

    public Task<TResult> UpdateAsync<TResult>(Func<Updater, Task<TResult>> updater, CancellationToken cancellationToken)
    {
        return _dispatcher.RunAsync(
            static x => x.updater(new(x.instance)),
            (updater, instance: this),
            cancellationToken).Unwrap();
    }

    public Task<TResult> UpdateAsync<TState, TResult>(Func<Updater, TState, Task<TResult>> updater, TState state, CancellationToken cancellationToken)
    {
        return _dispatcher.RunAsync(
            static x => x.updater(new(x.instance), x.state),
            (updater, state, instance: this),
            cancellationToken).Unwrap();
    }
}
