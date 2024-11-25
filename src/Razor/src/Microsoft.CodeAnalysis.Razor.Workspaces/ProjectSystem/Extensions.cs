// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#if !NET
using System;
#endif

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Serialization;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal static class Extensions
{
    public static DocumentSnapshotHandle ToHandle(this IDocumentSnapshot snapshot)
        => new(snapshot.FilePath, snapshot.TargetPath, snapshot.FileKind);

    public static ProjectKey ToProjectKey(this Project project)
    {
        var intermediateOutputPath = FilePathNormalizer.GetNormalizedDirectoryName(project.CompilationOutputInfo.AssemblyPath);
        return new(intermediateOutputPath);
    }

    /// <summary>
    /// Returns <see langword="true"/> if this <see cref="ProjectKey"/> matches the given <see cref="Project"/>.
    /// </summary>
    public static bool Matches(this ProjectKey projectKey, Project project)
    {
        // In order to perform this check, we are relying on the fact that Id will always end with a '/',
        // because it is guaranteed to be normalized. However, CompilationOutputInfo.AssemblyPath will
        // contain the assembly file name, which AreDirectoryPathsEquivalent will shave off before comparing.
        // So, AreDirectoryPathsEquivalent will return true when Id is "C:/my/project/path/"
        // and the assembly path is "C:\my\project\path\assembly.dll"

        Debug.Assert(projectKey.Id.EndsWith('/'), $"This method can't be called if {nameof(projectKey.Id)} is not a normalized directory path.");

        return FilePathNormalizer.AreDirectoryPathsEquivalent(projectKey.Id, project.CompilationOutputInfo.AssemblyPath);
    }

    public static ImmutableArray<string> GetImportDocumentTargetPaths(this RazorProjectEngine projectEngine, HostDocument hostDocument)
    {
        var targetPath = hostDocument.TargetPath;
        var fileKind = hostDocument.FileKind;
        var documentItem = projectEngine.FileSystem.GetItem(targetPath, fileKind);

        using var importItems = new PooledArrayBuilder<RazorProjectItem>();

        foreach (var projectFeature in projectEngine.ProjectFeatures)
        {
            if (projectFeature is not IImportProjectFeature importProjectFeature)
            {
                continue;
            }

            foreach (var importItem in importProjectFeature.GetImports(documentItem))
            {
                if (importItem.FilePath is not null)
                {
                    importItems.Add(importItem);
                }
            }
        }

        if (importItems.Count == 0)
        {
            return [];
        }

        // Target path looks like `Foo\\Bar.cshtml`
        using var targetPaths = new PooledArrayBuilder<string>(capacity: importItems.Count);

        foreach (var importItem in importItems)
        {
            // TODO: Rationalize this and clean up.
            var itemTargetPath = importItem.FilePath.Replace('/', '\\').TrimStart('\\');

            if (FilePathNormalizingComparer.Instance.Equals(itemTargetPath, targetPath))
            {
                // We've normalized the original importItem.FilePath into the HostDocument.TargetPath.
                // For instance, if the HostDocument.TargetPath was '/_Imports.razor' it'd be normalized
                // into '_Imports.razor'. The purpose of this method is to get the associated document
                // paths for a given import file (_Imports.razor / _ViewImports.cshtml); therefore,
                // an import importing itself doesn't make sense.
                continue;
            }

            targetPaths.Add(itemTargetPath);
        }

        return targetPaths.DrainToImmutable();
    }
}
