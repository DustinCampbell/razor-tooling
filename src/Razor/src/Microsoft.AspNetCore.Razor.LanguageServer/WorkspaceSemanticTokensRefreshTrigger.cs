// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.AspNetCore.Razor.LanguageServer;

/// <summary>
/// Sends a 'workspace\semanticTokens\refresh' request each time the project changes.
/// </summary>
internal class WorkspaceSemanticTokensRefreshTrigger : IRazorStartupService
{
    private readonly IWorkspaceSemanticTokensRefreshNotifier _publisher;
    private readonly RazorSolutionManager _solutionManager;

    public WorkspaceSemanticTokensRefreshTrigger(
        IWorkspaceSemanticTokensRefreshNotifier publisher,
        RazorSolutionManager solutionManager)
    {
        _publisher = publisher;
        _solutionManager = solutionManager;
        _solutionManager.Changed += SolutionManager_Changed;
    }

    // Does not handle C# files
    private void SolutionManager_Changed(object? sender, ProjectChangeEventArgs args)
    {
        // Don't send for a simple Document edit. The platform should re-request any range that
        // is edited and if a parameter or type change is made it should be reflected as a ProjectChanged.
        if (args.Kind != ProjectChangeKind.DocumentChanged)
        {
            _publisher.NotifyWorkspaceSemanticTokensRefresh();
        }
    }
}
