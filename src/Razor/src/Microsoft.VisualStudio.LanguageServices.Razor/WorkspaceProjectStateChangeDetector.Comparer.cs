// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.Razor;

internal partial class WorkspaceProjectStateChangeDetector
{
    private sealed class Comparer : IEqualityComparer<(ProjectId?, ProjectKey)>
    {
        public static readonly Comparer Instance = new();

        private Comparer()
        {
        }

        public bool Equals((ProjectId?, ProjectKey) x, (ProjectId?, ProjectKey) y)
        {
            return x.Equals(y);
        }

        public int GetHashCode((ProjectId?, ProjectKey) obj)
        {
            return obj.GetHashCode();
        }
    }
}
