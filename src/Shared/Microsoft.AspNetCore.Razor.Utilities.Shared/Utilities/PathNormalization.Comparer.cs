// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Utilities;

internal static partial class PathNormalization
{
    private sealed class Comparer : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y)
            => AreFilePathsEquivalent(x, y);

        public int GetHashCode(string obj)
            => PathNormalization.ComputeHashCode(obj);
    }
}
