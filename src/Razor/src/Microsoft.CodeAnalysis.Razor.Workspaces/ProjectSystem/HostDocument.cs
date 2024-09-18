// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal sealed record class HostDocument
{
    private static readonly StringComparer s_filePathComparer = FilePathComparer.Instance;

    public string FilePath { get; init; }
    public string TargetPath { get; init; }
    public string FileKind { get; init; }

    public HostDocument(string filePath, string targetPath, string? fileKind = null)
    {
        FilePath = filePath;
        TargetPath = targetPath;
        FileKind = fileKind ?? FileKinds.GetFileKindFromFilePath(filePath);
    }

    public bool Equals(HostDocument? other)
        => other is not null &&
           FileKind == other.FileKind &&
           s_filePathComparer.Equals(FilePath, other.FilePath) &&
           s_filePathComparer.Equals(TargetPath, other.TargetPath);

    public override int GetHashCode()
    {
        var hash = HashCodeCombiner.Start();
        hash.Add(FileKind);
        hash.Add(FilePath, s_filePathComparer);
        hash.Add(TargetPath, s_filePathComparer);

        return hash.CombinedHash;
    }
}
