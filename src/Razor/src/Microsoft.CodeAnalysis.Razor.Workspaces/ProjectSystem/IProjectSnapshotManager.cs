// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal interface IProjectSnapshotManager
{
    /// <summary>
    ///  Occurs when <see cref="CurrentSolution"/> changes before any subscribers to <see cref="Changed"/> are notified.
    /// </summary>
    event EventHandler<ProjectChangeEventArgs> PriorityChanged;

    /// <summary>
    ///  Occurs when <see cref="CurrentSolution"/> changes.
    /// </summary>
    event EventHandler<ProjectChangeEventArgs> Changed;

    /// <summary>
    ///  The current Razor <see cref="ISolutionSnapshot"/>.
    /// </summary>
    ISolutionSnapshot CurrentSolution { get; }

    /// <summary>
    ///  Determines whether the solution is being closed by the host.
    /// </summary>
    /// <returns>
    ///  Returns <see langword="true"/> if the solution is being closed; otherwise, <see langword="false"/>.
    /// </returns>
    bool IsSolutionClosing { get; }

    /// <summary>
    ///  Determines whether the given document is open in the host.
    /// </summary>
    /// <returns>
    ///  Returns <see langword="true"/> if the document is open; otherwise, <see langword="false"/>.
    /// </returns>
    bool IsDocumentOpen(string documentFilePath);

    /// <summary>
    ///  Gets an array of open document file paths.
    /// </summary>
    ImmutableArray<string> GetOpenDocuments();

    /// <summary>
    ///  Update the current solution.
    /// </summary>
    Task UpdateAsync(Action<ProjectSnapshotManager.Updater> updater, CancellationToken cancellationToken);

    /// <inheritdoc cref="UpdateAsync(Action{ProjectSnapshotManager.Updater}, CancellationToken)"/>
    Task UpdateAsync<TState>(Action<ProjectSnapshotManager.Updater, TState> updater, TState state, CancellationToken cancellationToken);

    /// <inheritdoc cref="UpdateAsync(Action{ProjectSnapshotManager.Updater}, CancellationToken)"/>
    Task<TResult> UpdateAsync<TResult>(Func<ProjectSnapshotManager.Updater, TResult> updater, CancellationToken cancellationToken);

    /// <inheritdoc cref="UpdateAsync(Action{ProjectSnapshotManager.Updater}, CancellationToken)"/>
    Task<TResult> UpdateAsync<TState, TResult>(Func<ProjectSnapshotManager.Updater, TState, TResult> updater, TState state, CancellationToken cancellationToken);

    /// <inheritdoc cref="UpdateAsync(Action{ProjectSnapshotManager.Updater}, CancellationToken)"/>
    Task UpdateAsync(Func<ProjectSnapshotManager.Updater, Task> updater, CancellationToken cancellationToken);

    /// <inheritdoc cref="UpdateAsync(Action{ProjectSnapshotManager.Updater}, CancellationToken)"/>
    Task UpdateAsync<TState>(Func<ProjectSnapshotManager.Updater, TState, Task> updater, TState state, CancellationToken cancellationToken);

    /// <inheritdoc cref="UpdateAsync(Action{ProjectSnapshotManager.Updater}, CancellationToken)"/>
    Task<TResult> UpdateAsync<TResult>(Func<ProjectSnapshotManager.Updater, Task<TResult>> updater, CancellationToken cancellationToken);

    /// <inheritdoc cref="UpdateAsync(Action{ProjectSnapshotManager.Updater}, CancellationToken)"/>
    Task<TResult> UpdateAsync<TState, TResult>(Func<ProjectSnapshotManager.Updater, TState, Task<TResult>> updater, TState state, CancellationToken cancellationToken);
}
