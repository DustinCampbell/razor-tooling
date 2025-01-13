// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.Razor;

internal partial class WorkspaceProjectStateChangeDetector
{
    private sealed class Comparer : IEqualityComparer<(Project?, RazorProject)>
    {
        public static readonly Comparer Instance = new();

        private Comparer()
        {
        }

        public bool Equals((Project?, RazorProject) x, (Project?, RazorProject) y)
        {
            var (_, projectX) = x;
            var (_, projectY) = y;

            return FilePathComparer.Instance.Equals(projectX.Key.Id, projectY.Key.Id);
        }

        public int GetHashCode((Project?, RazorProject) obj)
        {
            var (_, project) = obj;

            return FilePathComparer.Instance.GetHashCode(project.Key.Id);
        }
    }
}
