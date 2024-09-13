// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal sealed record HostDocument
{
    private static readonly StringComparer s_filePathComparer = FilePathComparer.Instance;

    public string FileKind { get; init; }
    public string FilePath { get; init; }
    public string TargetPath { get; init; }

    /// <summary>
    ///  Creates a new <see cref="HostDocument"/> instance.
    /// </summary>
    /// <param name="filePath">The file path of the document.</param>
    /// <param name="targetPath">The target path of the document.</param>
    /// <param name="fileKind">
    ///  The file kind from <see cref="FileKinds"/>. If <see langword="null"/>, the kind
    ///  will be computed by calling <see cref="FileKinds.GetFileKindFromFilePath(string)"/>.
    /// </param>
    public HostDocument(string filePath, string targetPath, string? fileKind = null)
    {
        FileKind = fileKind ?? FileKinds.GetFileKindFromFilePath(filePath);
        FilePath = filePath;
        TargetPath = targetPath;
    }

    public bool Equals(HostDocument? other)
    {
        return other is not null &&
               FileKind == other.FileKind &&
               s_filePathComparer.Equals(FilePath, other.FilePath) &&
               s_filePathComparer.Equals(TargetPath, other.TargetPath);
    }

    public override int GetHashCode()
    {
        var combiner = HashCodeCombiner.Start();
        combiner.Add(FileKind);
        combiner.Add(FilePath, s_filePathComparer);
        combiner.Add(TargetPath, s_filePathComparer);

        return combiner.CombinedHash;
    }
}
