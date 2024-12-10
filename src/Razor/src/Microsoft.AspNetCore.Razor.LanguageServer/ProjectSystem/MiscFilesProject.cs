// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;

internal static class MiscFilesProject
{
    public static string DirectoryPath { get; }
    public static HostProject HostProject { get; }

    static MiscFilesProject()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"));
        var filePath = Path.Combine(tempDirectory, "__MISC_RAZOR_PROJECT__");
        var normalizedPath = FilePathNormalizer.Normalize(filePath);

        DirectoryPath = tempDirectory;
        HostProject = new HostProject(
            normalizedPath,
            normalizedPath,
            FallbackRazorConfiguration.Latest,
            rootNamespace: null,
            "Miscellaneous Files");
    }

    public static bool IsMiscellaneousProject(this IProjectSnapshot project)
    {
        return project.Key == HostProject.Key;
    }
}
