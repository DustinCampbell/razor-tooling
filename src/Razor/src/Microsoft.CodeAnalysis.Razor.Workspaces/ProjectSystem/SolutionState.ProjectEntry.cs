// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal sealed partial class SolutionState
{
    private readonly struct ProjectEntry(ProjectState state)
    {
        public ProjectState State { get; } = state;
        public ProjectSnapshot Snapshot { get; } = new(state);
    }
}
