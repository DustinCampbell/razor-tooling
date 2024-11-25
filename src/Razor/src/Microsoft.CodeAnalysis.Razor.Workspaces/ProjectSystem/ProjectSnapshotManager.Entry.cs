// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal partial class ProjectSnapshotManager
{
    private readonly struct Entry(ProjectState state)
    {
        public ProjectState State => state;
        public ProjectSnapshot Snapshot { get; } = new(state);
    }
}
