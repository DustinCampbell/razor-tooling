// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.LanguageServer;

internal partial class OpenDocumentGenerator
{
    private sealed class Comparer : IEqualityComparer<IRazorDocument>
    {
        public static readonly Comparer Instance = new();

        private Comparer()
        {
        }

        public bool Equals(IRazorDocument? x, IRazorDocument? y)
        {
            if (x is null)
            {
                return y is null;
            }
            else if (y is null)
            {
                return false;
            }

            return x.Project.Key.Equals(y.Project.Key) &&
                FilePathComparer.Instance.Equals(x.FilePath, y.FilePath);
        }

        public int GetHashCode(IRazorDocument obj)
        {
            var hash = HashCodeCombiner.Start();
            hash.Add(obj.Project.Key.Id, FilePathComparer.Instance);
            hash.Add(obj.FileKind, FilePathComparer.Instance);
            return hash.CombinedHash;
        }
    }
}
