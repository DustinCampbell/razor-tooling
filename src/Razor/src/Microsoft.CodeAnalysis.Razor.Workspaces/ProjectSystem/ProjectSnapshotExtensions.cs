﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Serialization;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal static class ProjectSnapshotExtensions
{
    public static IRazorDocument? GetDocument(this IRazorProject project, string filePath)
        => project.TryGetDocument(filePath, out var result)
            ? result
            : null;

    public static IRazorDocument GetRequiredDocument(this IRazorProject project, string filePath)
        => project.GetDocument(filePath).AssumeNotNull();

    public static RazorDocument? GetDocument(this ProjectSnapshot project, string filePath)
        => project.TryGetDocument(filePath, out var result)
            ? result
            : null;

    public static RazorDocument GetRequiredDocument(this ProjectSnapshot project, string filePath)
        => project.GetDocument(filePath).AssumeNotNull();

    public static RazorProjectInfo ToRazorProjectInfo(this ProjectSnapshot project)
    {
        using var documents = new PooledArrayBuilder<DocumentSnapshotHandle>();

        foreach (var documentFilePath in project.DocumentFilePaths)
        {
            if (project.TryGetDocument(documentFilePath, out var document))
            {
                var documentHandle = document.ToHandle();

                documents.Add(documentHandle);
            }
        }

        return new RazorProjectInfo(
            projectKey: project.Key,
            filePath: project.FilePath,
            configuration: project.Configuration,
            rootNamespace: project.RootNamespace,
            displayName: project.DisplayName,
            projectWorkspaceState: project.ProjectWorkspaceState,
            documents: documents.DrainToImmutable());
    }
}
