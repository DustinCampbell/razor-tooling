// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal static class ProjectSnapshotManagerExtensions
{
    public static RazorProject? GetProject(this ProjectSnapshotManager projectManager, ProjectKey projectKey)
        => projectManager.TryGetProject(projectKey, out var result)
            ? result
            : null;

    public static RazorProject GetRequiredProject(this ProjectSnapshotManager projectManager, ProjectKey projectKey)
        => projectManager.GetProject(projectKey).AssumeNotNull();

    public static bool ContainsDocument(this ProjectSnapshotManager projectManager, ProjectKey projectKey, string documentFilePath)
        => projectManager.TryGetProject(projectKey, out var project) &&
           project.ContainsDocument(documentFilePath);

    public static bool TryGetDocument(
        this ProjectSnapshotManager projectManager,
        ProjectKey projectKey,
        string documentFilePath,
        [NotNullWhen(true)] out RazorDocument? result)
    {
        result = projectManager.TryGetProject(projectKey, out var project)
            ? project.GetDocument(documentFilePath)
            : null;

        return result is not null;
    }

    public static RazorDocument? GetDocument(this ProjectSnapshotManager projectManager, ProjectKey projectKey, string documentFilePath)
        => projectManager.TryGetDocument(projectKey, documentFilePath, out var result)
            ? result
            : null;

    public static RazorDocument GetRequiredDocument(this ProjectSnapshotManager projectManager, ProjectKey projectKey, string documentFilePath)
        => projectManager.GetDocument(projectKey, documentFilePath).AssumeNotNull();

    public static RazorProject? GetProject(this ProjectSnapshotManager.Updater updater, ProjectKey projectKey)
        => updater.TryGetProject(projectKey, out var result)
            ? result
            : null;

    public static RazorProject GetRequiredProject(this ProjectSnapshotManager.Updater updater, ProjectKey projectKey)
        => updater.GetProject(projectKey).AssumeNotNull();

    public static bool ContainsDocument(this ProjectSnapshotManager.Updater updater, ProjectKey projectKey, string documentFilePath)
        => updater.TryGetProject(projectKey, out var project) &&
           project.ContainsDocument(documentFilePath);

    public static bool TryGetDocument(
        this ProjectSnapshotManager.Updater updater,
        ProjectKey projectKey,
        string documentFilePath,
        [NotNullWhen(true)] out RazorDocument? result)
    {
        result = updater.TryGetProject(projectKey, out var project)
            ? project.GetDocument(documentFilePath)
            : null;

        return result is not null;
    }

    public static RazorDocument? GetDocument(this ProjectSnapshotManager.Updater updater, ProjectKey projectKey, string documentFilePath)
        => updater.TryGetDocument(projectKey, documentFilePath, out var result)
            ? result
            : null;

    public static RazorDocument GetRequiredDocument(this ProjectSnapshotManager.Updater updater, ProjectKey projectKey, string documentFilePath)
        => updater.GetDocument(projectKey, documentFilePath).AssumeNotNull();
}
