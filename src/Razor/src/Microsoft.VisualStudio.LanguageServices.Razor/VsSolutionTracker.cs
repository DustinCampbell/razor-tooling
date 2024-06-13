// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.Razor.Extensions;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Razor;

[Export(typeof(IRazorStartupService))]
internal class VsSolutionTracker : IRazorStartupService, IDisposable
{
    private readonly IProjectSnapshotManager _projectManager;
    private readonly JoinableTaskContext _joinableTaskContext;
    private readonly CancellationTokenSource _disposeTokenSource;

    [ImportingConstructor]
    public VsSolutionTracker(
       IProjectSnapshotManager projectManager,
       JoinableTaskContext joinableTaskContext)
    {
        _projectManager = projectManager;
        _joinableTaskContext = joinableTaskContext;
        _disposeTokenSource = new();

        var jtf = _joinableTaskContext.Factory;

        _ = jtf.RunAsync(async () =>
        {
            await jtf.SwitchToMainThreadAsync();

            SolutionEvents.OnBeforeOpenSolution += SolutionEvents_OnBeforeOpenSolution;
            SolutionEvents.OnAfterOpenSolution += SolutionEvents_OnAfterOpenSolution;
            SolutionEvents.OnBeforeCloseSolution += SolutionEvents_OnBeforeCloseSolution;
            SolutionEvents.OnAfterCloseSolution += SolutionEvents_OnAfterCloseSolution;
        });
    }

    public void Dispose()
    {
        if (_disposeTokenSource.IsCancellationRequested)
        {
            return;
        }

        _disposeTokenSource.Cancel();
        _disposeTokenSource.Dispose();

        _joinableTaskContext.AssertUIThread();

        SolutionEvents.OnBeforeOpenSolution -= SolutionEvents_OnBeforeOpenSolution;
        SolutionEvents.OnAfterOpenSolution -= SolutionEvents_OnAfterOpenSolution;
        SolutionEvents.OnBeforeCloseSolution -= SolutionEvents_OnBeforeCloseSolution;
        SolutionEvents.OnAfterCloseSolution -= SolutionEvents_OnAfterCloseSolution;
    }

    private void SolutionEvents_OnBeforeOpenSolution(object sender, BeforeOpenSolutionEventArgs e)
    {
        _projectManager.UpdateAsync(
            static updater => updater.SetSolutionState(SolutionState.Opening),
            _disposeTokenSource.Token).Forget();
    }

    private void SolutionEvents_OnAfterOpenSolution(object sender, OpenSolutionEventArgs e)
    {
        _projectManager.UpdateAsync(
            static updater => updater.SetSolutionState(SolutionState.Opened),
            _disposeTokenSource.Token).Forget();
    }

    private void SolutionEvents_OnBeforeCloseSolution(object sender, EventArgs e)
    {
        _projectManager.UpdateAsync(
            static updater => updater.SetSolutionState(SolutionState.Closing),
            _disposeTokenSource.Token).Forget();
    }

    private void SolutionEvents_OnAfterCloseSolution(object sender, EventArgs e)
    {
        _projectManager.UpdateAsync(
            static updater => updater.SetSolutionState(SolutionState.Closed),
            _disposeTokenSource.Token).Forget();
    }
}
