// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal partial class ProjectSnapshotManager
{
    private sealed record Entry(ProjectState State)
    {
        private RazorProject? _snapshotUnsafe;

        public RazorProject GetSnapshot()
        {
            return _snapshotUnsafe ??= new RazorProject(State);
        }
    }
}
