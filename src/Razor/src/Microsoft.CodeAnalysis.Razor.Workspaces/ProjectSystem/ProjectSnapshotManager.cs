// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

/// <summary>
///  <see cref="ProjectSnapshotManager"/> exposes the current Razor <see cref="ISolutionSnapshot"/>
///  and provides methods to mutate it and events that will be triggered when it changes.
/// </summary>
internal partial class ProjectSnapshotManager : IProjectSnapshotManager, IDisposable
{
    /// <summary>
    ///  Used to schedule updates that run in sequential order.
    /// </summary>
    /// <remarks>
    ///  If an update is scheduled when running on the dispatcher, the update will be run inline.
    /// </remarks>
    private readonly Dispatcher _dispatcher;

    /// <summary>
    ///  <see cref="ILogger"/> to use 
    /// </summary>
    private readonly ILogger _logger;

    #region protected by lock

    /// <summary>
    ///  A <see cref="ReaderWriterLockSlim"/> is used to avoid lock contention.
    /// </summary>
    private readonly ReaderWriterLockSlim _stateLock = new(LockRecursionPolicy.NoRecursion);

    /// <summary>
    ///  The current <see cref="SolutionSnapshot"/>.
    /// </summary>
    /// <remarks>
    ///  ⚠️ This field must be read/written under <see cref="_stateLock"/>.
    /// </remarks>
    private SolutionSnapshot _currentSolution;

    /// <summary>
    ///  <see langword="true"/> if the solution is being closed by the host; otherwise, <see langword="false"/>.
    /// </summary>
    /// <remarks>
    ///  ⚠️ This field must be read/written under <see cref="_stateLock"/>.
    /// </remarks>
    private bool _isSolutionClosing;

    /// <summary>
    ///  The set of open document file paths.
    /// </summary>
    /// <remarks>
    ///  ⚠️ This field must be read/written under <see cref="_stateLock"/>.
    /// </remarks>
    private readonly HashSet<string> _openDocumentSet = new(FilePathComparer.Instance);

    #endregion

    #region protected by dispatcher

    /// <summary>
    ///  A queue of ordered notifications to process.
    /// </summary>
    /// <remarks>
    ///  ⚠️ This field must only be accessed when running on the dispatcher.
    /// </remarks>
    private readonly Queue<ProjectChangeEventArgs> _notificationQueue = new();

    /// <summary>
    ///  <see langword="true"/> while <see cref="_notificationQueue"/> is being processed.
    /// </summary>
    /// <remarks>
    ///  ⚠️ This field must only be accessed when running on the dispatcher.
    /// </remarks>
    private bool _processingNotifications;

    #endregion

    /// <summary>
    ///  Constructs an instance of <see cref="ProjectSnapshotManager"/>.
    /// </summary>
    /// <param name="projectEngineFactoryProvider">
    ///  The <see cref="IProjectEngineFactoryProvider"/> to use for creating <see cref="RazorProjectEngine">project engines</see>.
    /// </param>
    /// <param name="languageServerFeatureOptions">The options that were used to start the language server</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use.</param>
    public ProjectSnapshotManager(
        IProjectEngineFactoryProvider projectEngineFactoryProvider,
        LanguageServerFeatureOptions languageServerFeatureOptions,
        ILoggerFactory loggerFactory)
    {
        _dispatcher = new(loggerFactory);
        _logger = loggerFactory.GetOrCreateLogger(GetType());

        var solutionState = SolutionState.Create(projectEngineFactoryProvider, languageServerFeatureOptions);
        var solution = new SolutionSnapshot(solutionState);
        _currentSolution = InitializeSolution(solution);
    }

    /// <summary>
    ///  Override to set up the initial set of projects and documents.
    /// </summary>
    protected virtual SolutionSnapshot InitializeSolution(SolutionSnapshot solution)
        => solution;

    public void Dispose()
    {
        _dispatcher.Dispose();
        _stateLock.Dispose();
    }

    /// <inheritdoc/>
    public event EventHandler<ProjectChangeEventArgs>? PriorityChanged;

    /// <inheritdoc/>
    public event EventHandler<ProjectChangeEventArgs>? Changed;

    /// <inheritdoc cref="IProjectSnapshotManager.CurrentSolution"/>
    public SolutionSnapshot CurrentSolution
    {
        get
        {
            using (_stateLock.DisposableRead())
            {
                return _currentSolution;
            }
        }
    }

    /// <inheritdoc/>
    ISolutionSnapshot IProjectSnapshotManager.CurrentSolution => CurrentSolution;

    /// <inheritdoc/>
    public bool IsSolutionClosing
    {
        get
        {
            using (_stateLock.DisposableRead())
            {
                return _isSolutionClosing;
            }
        }
    }

    /// <inheritdoc/>
    public ImmutableArray<string> GetOpenDocuments()
    {
        using (_stateLock.DisposableRead())
        {
            return [.. _openDocumentSet];
        }
    }

    /// <inheritdoc/>
    public bool IsDocumentOpen(string filePath)
    {
        using (_stateLock.DisposableRead())
        {
            return _openDocumentSet.Contains(filePath);
        }
    }

    private void DocumentAdded(ProjectKey projectKey, HostDocument hostDocument, TextLoader textLoader)
    {
        if (TryUpdateProject(
            projectKey,
            transformation: solution => solution.AddDocument(projectKey, hostDocument, textLoader),
            out var oldSnapshot,
            out var newSnapshot,
            out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.DocumentAdded(oldSnapshot, newSnapshot, hostDocument.FilePath, isSolutionClosing));
        }
    }

    private void DocumentRemoved(ProjectKey projectKey, HostDocument hostDocument)
    {
        if (TryUpdateProject(
            projectKey,
            transformation: solution => solution.RemoveDocument(projectKey, hostDocument.FilePath),
            out var oldSnapshot,
            out var newSnapshot,
            out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.DocumentRemoved(oldSnapshot, newSnapshot, hostDocument.FilePath, isSolutionClosing));
        }
    }

    private void DocumentOpened(ProjectKey projectKey, string documentFilePath, SourceText sourceText)
    {
        if (TryUpdateProject(
            projectKey,
            transformation: solution => solution.UpdateDocumentText(projectKey, documentFilePath, sourceText),
            onAfterUpdate: () => _openDocumentSet.Add(documentFilePath),
            out var oldSnapshot,
            out var newSnapshot,
            out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.DocumentChanged(oldSnapshot, newSnapshot, documentFilePath, isSolutionClosing));
        }
    }

    private void DocumentClosed(ProjectKey projectKey, string documentFilePath, TextLoader textLoader)
    {
        if (TryUpdateProject(
            projectKey,
            transformation: solution => solution.UpdateDocumentText(projectKey, documentFilePath, textLoader),
            onAfterUpdate: () => _openDocumentSet.Remove(documentFilePath),
            out var oldSnapshot,
            out var newSnapshot,
            out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.DocumentChanged(oldSnapshot, newSnapshot, documentFilePath, isSolutionClosing));
        }
    }

    private void DocumentChanged(ProjectKey projectKey, string documentFilePath, SourceText sourceText)
    {
        if (TryUpdateProject(
            projectKey,
            transformation: solution => solution.UpdateDocumentText(projectKey, documentFilePath, sourceText),
            out var oldSnapshot,
            out var newSnapshot,
            out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.DocumentChanged(oldSnapshot, newSnapshot, documentFilePath, isSolutionClosing));
        }
    }

    private void DocumentChanged(ProjectKey projectKey, string documentFilePath, TextLoader textLoader)
    {
        if (TryUpdateProject(
            projectKey,
            transformation: solution => solution.UpdateDocumentText(projectKey, documentFilePath, textLoader),
            out var oldSnapshot,
            out var newSnapshot,
            out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.DocumentChanged(oldSnapshot, newSnapshot, documentFilePath, isSolutionClosing));
        }
    }

    private void ProjectAdded(HostProject hostProject)
    {
        if (TryAddProject(hostProject, out var newSnapshot, out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.ProjectAdded(newSnapshot, isSolutionClosing));
        }
    }

    private void ProjectRemoved(ProjectKey projectKey)
    {
        if (TryRemoveProject(projectKey, out var oldSnapshot, out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.ProjectRemoved(oldSnapshot, isSolutionClosing));
        }
    }

    private void ProjectWorkspaceStateChanged(ProjectKey projectKey, ProjectWorkspaceState projectWorkspaceState)
    {
        if (TryUpdateProject(
            projectKey,
            transformation: solution => solution.UpdateProjectWorkspaceState(projectKey, projectWorkspaceState),
            out var oldSnapshot,
            out var newSnapshot,
            out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.ProjectChanged(oldSnapshot, newSnapshot, isSolutionClosing));
        }
    }

    private void ProjectConfigurationChanged(HostProject hostProject)
    {
        if (TryUpdateProject(
            hostProject.Key,
            transformation: solution => solution.UpdateProjectConfiguration(hostProject),
            out var oldSnapshot,
            out var newSnapshot,
            out var isSolutionClosing))
        {
            NotifyListeners(ProjectChangeEventArgs.ProjectChanged(oldSnapshot, newSnapshot, isSolutionClosing));
        }
    }

    private void SolutionOpened()
    {
        _dispatcher.AssertRunningOnDispatcher();

        using (_stateLock.DisposableWrite())
        {
            _isSolutionClosing = false;
        }
    }

    private void SolutionClosed()
    {
        _dispatcher.AssertRunningOnDispatcher();

        using (_stateLock.DisposableWrite())
        {
            _isSolutionClosing = true;
        }
    }

    private bool TryAddProject(
        HostProject hostProject,
        [NotNullWhen(true)] out IProjectSnapshot? newSnapshot,
        out bool isSolutionClosing)
    {
        _dispatcher.AssertRunningOnDispatcher();

        SolutionSnapshot newSolution;

        using (var upgradeableLock = _stateLock.DisposableUpgradeableRead())
        {
            isSolutionClosing = _isSolutionClosing;

            // Don't bother adding a project if the solution is closing.
            if (isSolutionClosing)
            {
                newSnapshot = null;
                return false;
            }

            var oldSolution = _currentSolution;
            newSolution = oldSolution.AddProject(hostProject);

            if (ReferenceEquals(oldSolution, newSolution))
            {
                newSnapshot = null;
                return false;
            }

            upgradeableLock.EnterWrite();
            _currentSolution = newSolution;
        }

        newSnapshot = newSolution.GetRequiredProject(hostProject.Key);
        return true;
    }

    private bool TryRemoveProject(
        ProjectKey projectKey,
        [NotNullWhen(true)] out IProjectSnapshot? oldSnapshot,
        out bool isSolutionClosing)
    {
        _dispatcher.AssertRunningOnDispatcher();

        using var upgradeableLock = _stateLock.DisposableUpgradeableRead();

        isSolutionClosing = _isSolutionClosing;

        var oldSolution = _currentSolution;
        oldSnapshot = oldSolution.GetRequiredProject(projectKey);

        // Don't remove a project if the solution is closing.
        if (isSolutionClosing)
        {
            return true;
        }

        var newSolution = oldSolution.RemoveProject(projectKey);

        if (!ReferenceEquals(oldSolution, newSolution))
        {
            upgradeableLock.EnterWrite();
            _currentSolution = newSolution;
        }

        return true;
    }

    private bool TryUpdateProject(
        ProjectKey projectKey,
        Func<SolutionSnapshot, SolutionSnapshot> transformation,
        [NotNullWhen(true)] out IProjectSnapshot? oldSnapshot,
        [NotNullWhen(true)] out IProjectSnapshot? newSnapshot,
        out bool isSolutionClosing)
        => TryUpdateProject(projectKey, transformation, onAfterUpdate: null, out oldSnapshot, out newSnapshot, out isSolutionClosing);

    private bool TryUpdateProject(
        ProjectKey projectKey,
        Func<SolutionSnapshot, SolutionSnapshot> transformation,
        Action? onAfterUpdate,
        [NotNullWhen(true)] out IProjectSnapshot? oldSnapshot,
        [NotNullWhen(true)] out IProjectSnapshot? newSnapshot,
        out bool isSolutionClosing)
    {
        _dispatcher.AssertRunningOnDispatcher();

        SolutionSnapshot oldSolution;
        SolutionSnapshot newSolution;

        using (var upgradeableLock = _stateLock.DisposableUpgradeableRead())
        {
            isSolutionClosing = _isSolutionClosing;
            oldSolution = _currentSolution;

            // If the solution is closing we don't need to bother computing new state
            if (isSolutionClosing)
            {
                oldSnapshot = newSnapshot = oldSolution.GetRequiredProject(projectKey);
                return true;
            }

            newSolution = transformation(oldSolution);

            if (ReferenceEquals(oldSolution, newSolution))
            {
                oldSnapshot = newSnapshot = null;
                return false;
            }

            upgradeableLock.EnterWrite();

            _currentSolution = newSolution;
            onAfterUpdate?.Invoke();
        }

        oldSnapshot = oldSolution.GetRequiredProject(projectKey);
        newSnapshot = newSolution.GetRequiredProject(projectKey);

        return true;
    }

    private void NotifyListeners(ProjectChangeEventArgs args)
    {
        // Notifications are *always* sent using the dispatcher.
        // This ensures that _notificationQueue and _processingNotifications are synchronized.
        _dispatcher.AssertRunningOnDispatcher();

        // Enqueue the latest notification.
        _notificationQueue.Enqueue(args);

        // We're already processing the notification queue, so we're done.
        if (_processingNotifications)
        {
            return;
        }

        Debug.Assert(_notificationQueue.Count == 1, "There should only be a single queued notification when it processing begins.");

        // The notification queue is processed when it contains *exactly* one notification.
        // Note that a notification subscriber may mutate the current solution and cause additional
        // notifications to be be enqueued. However, because we are already running on the dispatcher,
        // those updates will occur synchronously.

        _processingNotifications = true;
        try
        {
            while (_notificationQueue.Count > 0)
            {
                var currentArgs = _notificationQueue.Dequeue();

                PriorityChanged?.Invoke(this, currentArgs);
                Changed?.Invoke(this, currentArgs);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending notifications.");
        }
        finally
        {
            _processingNotifications = false;
        }
    }

    /// <inheritdoc/>
    public Task UpdateAsync(Action<Updater> updater, CancellationToken cancellationToken)
    {
        return _dispatcher.RunAsync(
            static x => x.updater(new(x.instance)),
            (updater, instance: this),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task UpdateAsync<TState>(Action<Updater, TState> updater, TState state, CancellationToken cancellationToken)
    {
        return _dispatcher.RunAsync(
            static x => x.updater(new(x.instance), x.state),
            (updater, state, instance: this),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TResult> UpdateAsync<TResult>(Func<Updater, TResult> updater, CancellationToken cancellationToken)
    {
        return _dispatcher.RunAsync(
            static x => x.updater(new(x.instance)),
            (updater, instance: this),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TResult> UpdateAsync<TState, TResult>(Func<Updater, TState, TResult> updater, TState state, CancellationToken cancellationToken)
    {
        return _dispatcher.RunAsync(
            static x => x.updater(new(x.instance), x.state),
            (updater, state, instance: this),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(Func<Updater, Task> updater, CancellationToken cancellationToken)
    {
        return _dispatcher.RunAsync(
            static x => x.updater(new(x.instance)),
            (updater, instance: this),
            cancellationToken).Unwrap();
    }

    /// <inheritdoc/>
    public Task UpdateAsync<TState>(Func<Updater, TState, Task> updater, TState state, CancellationToken cancellationToken)
    {
        return _dispatcher.RunAsync(
            static x => x.updater(new(x.instance), x.state),
            (updater, state, instance: this),
            cancellationToken).Unwrap();
    }

    /// <inheritdoc/>
    public Task<TResult> UpdateAsync<TResult>(Func<Updater, Task<TResult>> updater, CancellationToken cancellationToken)
    {
        return _dispatcher.RunAsync(
            static x => x.updater(new(x.instance)),
            (updater, instance: this),
            cancellationToken).Unwrap();
    }

    /// <inheritdoc/>
    public Task<TResult> UpdateAsync<TState, TResult>(Func<Updater, TState, Task<TResult>> updater, TState state, CancellationToken cancellationToken)
    {
        return _dispatcher.RunAsync(
            static x => x.updater(new(x.instance), x.state),
            (updater, state, instance: this),
            cancellationToken).Unwrap();
    }
}
