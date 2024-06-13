// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.Razor;

internal partial class WorkspaceProjectStateChangeDetector
{
    private sealed class Comparer : IEqualityComparer<Work>
    {
        public static readonly Comparer Instance = new();

        private Comparer()
        {
        }

        public bool Equals(Work x, Work y) => (x, y) switch
        {
            (Update { Key: var keyX }, Update { Key: var keyY }) => keyX == keyY,
            (Remove { Key: var keyX }, Remove { Key: var keyY }) => keyX == keyY,
            _ => false,
        };

        public int GetHashCode(Work obj) => obj.Key.GetHashCode();
    }
}
