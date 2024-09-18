// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;

internal sealed record class MiscFilesHostProject : HostProject
{
    public static MiscFilesHostProject Instance { get; } = Create();

    public static bool IsMiscellaneousProject(IProjectSnapshot project)
    {
        return project.Key == Instance.Key;
    }

    public string DirectoryPath { get; }

    private MiscFilesHostProject(
        string directory,
        string filePath,
        string intermediateOutputPath,
        RazorConfiguration configuration,
        string? rootNamespace,
        string displayName)
        : base(filePath, intermediateOutputPath, configuration, rootNamespace, displayName)
    {
        DirectoryPath = directory;
    }

    public bool Equals(MiscFilesHostProject? other)
        => base.Equals(other) &&
           FilePathComparer.Instance.Equals(DirectoryPath, other.DirectoryPath);

    public override int GetHashCode()
    {
        var hash = HashCodeCombiner.Start();
        hash.Add(base.GetHashCode());
        hash.Add(DirectoryPath, FilePathComparer.Instance);

        return hash.CombinedHash;
    }

    private static MiscFilesHostProject Create()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"));
        var miscellaneousProjectPath = Path.Combine(tempDirectory, "__MISC_RAZOR_PROJECT__");
        var normalizedPath = FilePathNormalizer.Normalize(miscellaneousProjectPath);

        return new MiscFilesHostProject(
            tempDirectory,
            normalizedPath,
            normalizedPath,
            FallbackRazorConfiguration.Latest,
            rootNamespace: null,
            "Miscellaneous Files");
    }
}
