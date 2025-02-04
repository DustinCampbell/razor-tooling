// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
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

    /// <summary>
    ///  Computes a Razor target path (e.g. 'Components\Pages\Error.razor') from a <see cref="RazorProjectItem.FilePath"/>.
    /// </summary>
    /// 
    /// <remarks>
    ///  <see cref="RazorProjectItem.FilePath "/> is defined as relative to the project root with a leading '/'
    ///  and all other slashes normalized to '/'.
    /// </remarks>
    public static string? GetTargetPathFromFilePath(this RazorProjectItem projectItem)
    {
        var filePath = projectItem.FilePath;

        // Some RazorProjectItems have a null FilePath, such as default imports.
        if (filePath is null)
        {
            return null;
        }

        var length = filePath.Length;
        var startIndex = 0;

        // RazorProjectItem.FilePath *should* start with a '/', but we'll check just in case.
        if (filePath.StartsWith('/'))
        {
            startIndex++;
            length--;
        }

        // Is there nothing left? If so, skip it.
        if (length == 0)
        {
            return null;
        }

        return StringFactory.Create(length, state: (filePath, startIndex), static (span, state) =>
        {
            var filePath = state.filePath.AsSpan(state.startIndex);

            while (!filePath.IsEmpty)
            {
                // Find the next slash.
                var slashIndex = filePath.IndexOf('/');

                // If there aren't anymore slashes, copy the remaining file path.
                if (slashIndex < 0)
                {
                    filePath.CopyTo(span);
                    span = span[filePath.Length..];
                    break;
                }

                filePath[..slashIndex].CopyTo(span);

                filePath = filePath[(slashIndex + 1)..];
                span = span[slashIndex..];

                span[0] = '\\';
                span = span[1..];
            }

            Debug.Assert(span.IsEmpty);
        });
    }
}
