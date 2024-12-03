// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly Dispatcher _dispatcher;
    private readonly bool _initialized;

    public event EventHandler<ProjectChangeEventArgs>? PriorityChanged;
    public event EventHandler<ProjectChangeEventArgs>? Changed;

    private readonly ReaderWriterLockSlim _readerWriterLock = new(LockRecursionPolicy.NoRecursion);

    private SolutionSnapshot _currentSolution;

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
        var solutionState = SolutionState.Create(projectEngineFactoryProvider, languageServerFeatureOptions);
        _currentSolution = new SolutionSnapshot(solutionState);

        _dispatcher = new(loggerFactory);

        initializer?.Invoke(new(this));

        _initialized = true;
    }

    public void Dispose()
    {
        _dispatcher.Dispose();
        _readerWriterLock.Dispose();
    }

    public ISolutionSnapshot CurrentSolution
        => CurrentSolutionSnapshot;

    private SolutionSnapshot CurrentSolutionSnapshot
        => Volatile.Read(ref _currentSolution);

    public bool IsSolutionClosing
        => CurrentSolutionSnapshot.IsSolutionClosing;

    public ImmutableArray<string> GetOpenDocuments()
        => CurrentSolutionSnapshot.GetOpenDocuments();

    public ImmutableArray<ProjectKey> GetAllProjectKeys(string projectFilePath)
        => CurrentSolutionSnapshot.GetAllProjectKeys(projectFilePath);

    public bool IsDocumentOpen(string documentFilePath)
        => CurrentSolutionSnapshot.IsDocumentOpen(documentFilePath);

    private void DocumentAdded(ProjectKey projectKey, HostDocument hostDocument, TextLoader textLoader)
    {
        if (_initialized)
        {
            _dispatcher.AssertRunningOnDispatcher();
        }

        if (TryUpdateProject(
            projectKey,
            solutionStateUpdater: state => state.AddDocument(projectKey, hostDocument, textLoader),
            out var oldSnapshot,
            out var newSnapshot,
            out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.DocumentAdded(oldSnapshot, newSnapshot, hostDocument.FilePath, isSolutionClosing));
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
            solutionStateUpdater: state => state.RemoveDocument(projectKey, hostDocument.FilePath),
            out var oldSnapshot,
            out var newSnapshot,
            out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.DocumentRemoved(oldSnapshot, newSnapshot, hostDocument.FilePath, isSolutionClosing));
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
            solutionStateUpdater: state => state.OpenDocument(projectKey, documentFilePath, sourceText),
            out var oldSnapshot,
            out var newSnapshot,
            out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.DocumentChanged(oldSnapshot, newSnapshot, documentFilePath, isSolutionClosing));
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
            solutionStateUpdater: state => state.CloseDocument(projectKey, documentFilePath, textLoader),
            out var oldSnapshot,
            out var newSnapshot,
            out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.DocumentChanged(oldSnapshot, newSnapshot, documentFilePath, isSolutionClosing));
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
            solutionStateUpdater: state => state.UpdateDocumentText(projectKey, documentFilePath, sourceText),
            out var oldSnapshot,
            out var newSnapshot,
            out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.DocumentChanged(oldSnapshot, newSnapshot, documentFilePath, isSolutionClosing));
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
            solutionStateUpdater: state => state.UpdateDocumentText(projectKey, documentFilePath, textLoader),
            out var oldSnapshot,
            out var newSnapshot,
            out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.DocumentChanged(oldSnapshot, newSnapshot, documentFilePath, isSolutionClosing));
        }
    }

    private void ProjectAdded(HostProject hostProject)
    {
        if (_initialized)
        {
            _dispatcher.AssertRunningOnDispatcher();
        }

        if (TryAddProject(hostProject, out var newSnapshot, out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.ProjectAdded(newSnapshot, isSolutionClosing));
        }
    }

    private void ProjectRemoved(ProjectKey projectKey)
    {
        if (_initialized)
        {
            _dispatcher.AssertRunningOnDispatcher();
        }

        if (TryRemoveProject(projectKey, out var oldSnapshot, out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.ProjectRemoved(oldSnapshot, isSolutionClosing));
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
            solutionStateUpdater: state => state.UpdateProjectWorkspaceState(projectKey, projectWorkspaceState),
            out var oldSnapshot,
            out var newSnapshot,
            out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.ProjectChanged(oldSnapshot, newSnapshot, isSolutionClosing));
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
            solutionStateUpdater: state => state.UpdateProjectConfiguration(hostProject),
            out var oldSnapshot,
            out var newSnapshot,
            out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.ProjectChanged(oldSnapshot, newSnapshot, isSolutionClosing));
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
            _currentSolution = _currentSolution.UpdateIsSolutionClosing(false);
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
            _currentSolution = _currentSolution.UpdateIsSolutionClosing(true);
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

    private bool TryAddProject(
        HostProject hostProject,
        [NotNullWhen(true)] out IProjectSnapshot? newSnapshot,
        out bool isSolutionClosing)
    {
        using var upgradeableLock = _readerWriterLock.DisposableUpgradeableRead();

        var oldSolution = _currentSolution;
        var newSolution = oldSolution.AddProject(hostProject);

        if (ReferenceEquals(oldSolution, newSolution))
        {
            newSnapshot = null;
            isSolutionClosing = oldSolution.IsSolutionClosing;
            return false;
        }

        upgradeableLock.EnterWrite();

        _currentSolution = newSolution;

        isSolutionClosing = newSolution.IsSolutionClosing;
        newSnapshot = newSolution.GetRequiredProject(hostProject.Key);
        return true;
    }

    private bool TryRemoveProject(
        ProjectKey projectKey,
        [NotNullWhen(true)] out IProjectSnapshot? oldSnapshot,
        out bool isSolutionClosing)
    {
        using var upgradeableLock = _readerWriterLock.DisposableUpgradeableRead();

        var oldState = _currentSolution;

        // If the solution is closing we don't need to bother computing new state
        if (!oldState.IsSolutionClosing)
        {
            var newState = oldState.RemoveProject(projectKey);

            if (!ReferenceEquals(oldState, newState))
            {
                upgradeableLock.EnterWrite();

                _currentSolution = newState;
            }
        }

        isSolutionClosing = oldState.IsSolutionClosing;
        oldSnapshot = oldState.GetRequiredProject(projectKey);
        return true;
    }

    private bool TryUpdateProject(
        ProjectKey projectKey,
        Func<SolutionSnapshot, SolutionSnapshot> solutionStateUpdater,
        [NotNullWhen(true)] out IProjectSnapshot? oldSnapshot,
        [NotNullWhen(true)] out IProjectSnapshot? newSnapshot,
        out bool isSolutionClosing)
    {
        using var upgradeableLock = _readerWriterLock.DisposableUpgradeableRead();

        var oldSolution = _currentSolution;

        // If the solution is closing we don't need to bother computing new state
        if (oldSolution.IsSolutionClosing)
        {
            isSolutionClosing = true;
            oldSnapshot = newSnapshot = oldSolution.GetRequiredProject(projectKey);
            return true;
        }

        var newSolution = solutionStateUpdater(oldSolution);

        if (ReferenceEquals(oldSolution, newSolution))
        {
            isSolutionClosing = oldSolution.IsSolutionClosing;
            oldSnapshot = newSnapshot = null;
            return false;
        }

        upgradeableLock.EnterWrite();

        _currentSolution = newSolution;

        isSolutionClosing = newSolution.IsSolutionClosing;
        oldSnapshot = oldSolution.GetRequiredProject(projectKey);
        newSnapshot = newSolution.GetRequiredProject(projectKey);

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
