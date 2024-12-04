// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal partial class DocumentState
{
    private class ComputedStateTracker(ComputedStateTracker? older = null)
    {
        private readonly object _lock = new();

        private ComputedStateTracker? _older = older;

        // We utilize a WeakReference here to avoid bloating committed memory. If pieces request document output inbetween GC collections
        // then we will provide the weak referenced task; otherwise we require any state requests to be re-computed.
        private WeakReference<Task<OutputAndVersion>>? _taskUnsafeReference;

        private OutputAndVersion? _outputAndVersion;

        public bool TryGetGeneratedOutputAndVersion([NotNullWhen(true)] out OutputAndVersion? result)
        {
            if (_outputAndVersion is { } outputAndVersion)
            {
                result = outputAndVersion;
                return true;
            }

            result = default;
            return false;
        }

        public ValueTask<OutputAndVersion> GetGeneratedOutputAndVersionAsync(
            DocumentSnapshot document,
            CancellationToken cancellationToken)
        {
            return TryGetGeneratedOutputAndVersion(out var result)
                ? new(result)
                : GetGeneratedOutputAndVersionCoreAsync(document, cancellationToken);

            async ValueTask<OutputAndVersion> GetGeneratedOutputAndVersionCoreAsync(DocumentSnapshot document, CancellationToken cancellationToken)
            {
                var result = await GetMemoizedGeneratedOutputAndVersionAsync(document, cancellationToken).ConfigureAwait(false);

                return InterlockedOperations.Initialize(ref _outputAndVersion, result);
            }
        }

        private Task<OutputAndVersion> GetMemoizedGeneratedOutputAndVersionAsync(
            DocumentSnapshot document,
            CancellationToken cancellationToken)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (_taskUnsafeReference is null ||
                !_taskUnsafeReference.TryGetTarget(out var taskUnsafe))
            {
                TaskCompletionSource<OutputAndVersion>? tcs = null;

                lock (_lock)
                {
                    if (_taskUnsafeReference is null ||
                        !_taskUnsafeReference.TryGetTarget(out taskUnsafe))
                    {
                        // So this is a bit confusing. Instead of directly calling the Razor parser inside of this lock we create an indirect TaskCompletionSource
                        // to represent when it completes. The reason behind this is that there are several scenarios in which the Razor parser will run synchronously
                        // (mostly all in VS) resulting in this lock being held for significantly longer than expected. To avoid threads queuing up repeatedly on the
                        // above lock and blocking we can allow those threads to await asynchronously for the completion of the original parse.

                        tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                        taskUnsafe = tcs.Task;
                        _taskUnsafeReference = new WeakReference<Task<OutputAndVersion>>(taskUnsafe);
                    }
                }

                if (tcs is null)
                {
                    // There's no task completion source created meaning a value was retrieved from cache, just return it.
                    return taskUnsafe;
                }

                // Typically in VS scenarios this will run synchronously because all resources are readily available.
                var outputTask = ComputeGeneratedOutputAndVersionAsync(document, cancellationToken);
                if (outputTask.IsCompleted)
                {
                    // Compiling ran synchronously, lets just immediately propagate to the TCS
                    PropagateToTaskCompletionSource(outputTask, tcs);
                }
                else
                {
                    // Task didn't run synchronously (most likely outside of VS), lets allocate a bit more but utilize ContinueWith
                    // to properly connect the output task and TCS
                    _ = outputTask.ContinueWith(
                        static (task, state) =>
                        {
                            Assumes.NotNull(state);
                            var tcs = (TaskCompletionSource<OutputAndVersion>)state;

                            PropagateToTaskCompletionSource(task, tcs);
                        },
                        tcs,
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }
            }

            return taskUnsafe;

            static void PropagateToTaskCompletionSource(
                Task<OutputAndVersion> targetTask,
                TaskCompletionSource<OutputAndVersion> tcs)
            {
                if (targetTask.Status == TaskStatus.RanToCompletion)
                {
#pragma warning disable VSTHRD103 // Call async methods when in an async method
                    tcs.SetResult(targetTask.Result);
#pragma warning restore VSTHRD103 // Call async methods when in an async method
                }
                else if (targetTask.Status == TaskStatus.Canceled)
                {
                    tcs.SetCanceled();
                }
                else if (targetTask.Status == TaskStatus.Faulted)
                {
                    // Faulted tasks area always aggregate exceptions so we need to extract the "true" exception if it's available:
                    Assumes.NotNull(targetTask.Exception);
                    var exception = targetTask.Exception.InnerException ?? targetTask.Exception;
                    tcs.SetException(exception);
                }
            }
        }

        private async Task<OutputAndVersion> ComputeGeneratedOutputAndVersionAsync(DocumentSnapshot document, CancellationToken cancellationToken)
        {
            // We only need to produce the generated code if any of our inputs is newer than the
            // previously cached output.
            //
            // First find the versions that are the inputs:
            // - The project + computed state
            // - The imports
            // - This document
            //
            // All of these things are cached, so no work is wasted if we do need to generate the code.
            var project = document.Project;
            var configurationVersion = project.ConfigurationVersion;
            var projectWorkspaceStateVersion = project.ProjectWorkspaceStateVersion;
            var documentCollectionVersion = project.DocumentCollectionVersion;
            var importItems = await GetImportItemsAsync(document, project.GetProjectEngine(), cancellationToken).ConfigureAwait(false);
            var documentVersion = await document.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);

            // OK now that have the previous output and all of the versions, we can see if anything
            // has changed that would require regenerating the code.
            var inputVersion = documentVersion;
            if (inputVersion.GetNewerVersion(configurationVersion) == configurationVersion)
            {
                inputVersion = configurationVersion;
            }

            if (inputVersion.GetNewerVersion(projectWorkspaceStateVersion) == projectWorkspaceStateVersion)
            {
                inputVersion = projectWorkspaceStateVersion;
            }

            if (inputVersion.GetNewerVersion(documentCollectionVersion) == documentCollectionVersion)
            {
                inputVersion = documentCollectionVersion;
            }

            foreach (var import in importItems)
            {
                var importVersion = import.Version;
                if (inputVersion.GetNewerVersion(importVersion) == importVersion)
                {
                    inputVersion = importVersion;
                }
            }

            if (_older?._taskUnsafeReference != null &&
                _older._taskUnsafeReference.TryGetTarget(out var taskUnsafe))
            {
                var olderOutputAndVersion = await taskUnsafe.ConfigureAwait(false);
                if (inputVersion.GetNewerVersion(olderOutputAndVersion.Version) == olderOutputAndVersion.Version)
                {
                    // Nothing has changed, we can use the cached result.
                    lock (_lock)
                    {
                        _taskUnsafeReference = _older._taskUnsafeReference;
                        _older = null;
                        return olderOutputAndVersion;
                    }
                }
            }

            var forceRuntimeCodeGeneration = project.LanguageServerFeatureOptions.ForceRuntimeCodeGeneration;
            var codeDocument = await GenerateCodeDocumentAsync(document, project.GetProjectEngine(), importItems, forceRuntimeCodeGeneration, cancellationToken).ConfigureAwait(false);
            return new(codeDocument, inputVersion);
        }
    }
}
