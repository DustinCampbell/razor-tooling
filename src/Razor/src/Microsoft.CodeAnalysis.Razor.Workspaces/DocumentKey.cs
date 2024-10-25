// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor;

internal readonly record struct DocumentKey
{
    public ProjectKey ProjectKey { get; }
    public string DocumentFilePath { get; }

    public DocumentKey(ProjectKey projectKey, string documentFilePath)
    {
        ProjectKey = projectKey;
        DocumentFilePath = documentFilePath;
    }

    public bool Equals(DocumentKey other)
        => ProjectKey.Equals(other.ProjectKey) &&
           FilePath.Comparer.Equals(DocumentFilePath, other.DocumentFilePath);

    public override int GetHashCode()
    {
        var hash = HashCodeCombiner.Start();
        hash.Add(ProjectKey);
        hash.Add(DocumentFilePath, FilePath.Comparer);
        return hash;
    }
}
