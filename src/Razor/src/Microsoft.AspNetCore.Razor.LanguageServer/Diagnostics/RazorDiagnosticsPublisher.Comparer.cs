// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Diagnostics;

internal partial class RazorDiagnosticsPublisher
{
    private sealed class Comparer : IEqualityComparer<IDocumentSnapshot>
    {
        public static readonly Comparer Instance = new();

        private Comparer()
        {
        }

        public bool Equals(IDocumentSnapshot? x, IDocumentSnapshot? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x.FilePath == y.FilePath;
        }

        public int GetHashCode(IDocumentSnapshot obj)
            => obj.FilePath.GetHashCode();
    }
}
