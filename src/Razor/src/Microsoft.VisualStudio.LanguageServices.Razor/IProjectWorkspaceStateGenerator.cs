// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.Razor;

internal interface IProjectWorkspaceStateGenerator
{
    void EnqueueUpdate(Project workspaceProject, IProjectSnapshot projectSnapshot, ProjectUpdateReason reason);
    void EnqueueRemove(ProjectKey key, ProjectUpdateReason reason);

    void CancelAllUpdates();
}
