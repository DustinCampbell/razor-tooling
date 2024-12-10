// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal static class ProjectSnapshotManagerExtensions
{
    public static IProjectSnapshot? GetProject(this IProjectSnapshotManager projectManager, ProjectKey projectKey)
        => projectManager.TryGetProject(projectKey, out var result)
            ? result
            : null;

    public static IProjectSnapshot GetRequiredProject(this IProjectSnapshotManager projectManager, ProjectKey projectKey)
        => projectManager.GetProject(projectKey).AssumeNotNull();

    public static bool ContainsDocument(this IProjectSnapshotManager projectManager, ProjectKey projectKey, string filePath)
        => projectManager.TryGetProject(projectKey, out var project) &&
           project.ContainsDocument(filePath);

    public static bool TryGetDocument(this IProjectSnapshotManager projectManager, ProjectKey projectKey, string filePath, [NotNullWhen(true)] out IDocumentSnapshot? document)
    {
        if (projectManager.TryGetProject(projectKey, out var project))
        {
            return project.TryGetDocument(filePath, out document);
        }

        document = null;
        return false;
    }

    public static IDocumentSnapshot? GetDocument(this IProjectSnapshotManager projectManager, ProjectKey projectKey, string filePath)
        => projectManager.TryGetDocument(projectKey, filePath, out var result)
            ? result
            : null;

    public static IDocumentSnapshot GetRequiredDocument(this IProjectSnapshotManager projectManager, ProjectKey projectKey, string filePath)
        => projectManager.GetDocument(projectKey, filePath).AssumeNotNull();

    public static ProjectSnapshot? GetProject(this ProjectSnapshotManager projectManager, ProjectKey projectKey)
        => projectManager.TryGetProject(projectKey, out var result)
            ? result
            : null;

    public static ProjectSnapshot GetRequiredProject(this ProjectSnapshotManager projectManager, ProjectKey projectKey)
        => projectManager.GetProject(projectKey).AssumeNotNull();

    public static bool ContainsDocument(this ProjectSnapshotManager projectManager, ProjectKey projectKey, string filePath)
        => projectManager.TryGetProject(projectKey, out var project) &&
           project.ContainsDocument(filePath);

    public static bool TryGetDocument(this ProjectSnapshotManager projectManager, ProjectKey projectKey, string filePath, [NotNullWhen(true)] out DocumentSnapshot? document)
    {
        if (projectManager.TryGetProject(projectKey, out var project))
        {
            return project.TryGetDocument(filePath, out document);
        }

        document = null;
        return false;
    }

    public static DocumentSnapshot? GetDocument(this ProjectSnapshotManager projectManager, ProjectKey projectKey, string filePath)
        => projectManager.TryGetDocument(projectKey, filePath, out var result)
            ? result
            : null;

    public static DocumentSnapshot GetRequiredDocument(this ProjectSnapshotManager projectManager, ProjectKey projectKey, string filePath)
        => projectManager.GetDocument(projectKey, filePath).AssumeNotNull();

    public static ProjectSnapshot? GetProject(this ProjectSnapshotManager.Updater updater, ProjectKey projectKey)
        => updater.TryGetProject(projectKey, out var result)
            ? result
            : null;

    public static ProjectSnapshot GetRequiredProject(this ProjectSnapshotManager.Updater updater, ProjectKey projectKey)
        => updater.GetProject(projectKey).AssumeNotNull();

    public static bool ContainsDocument(this ProjectSnapshotManager.Updater updater, ProjectKey projectKey, string filePath)
        => updater.TryGetProject(projectKey, out var project) &&
           project.ContainsDocument(filePath);

    public static bool TryGetDocument(this ProjectSnapshotManager.Updater updater, ProjectKey projectKey, string filePath, [NotNullWhen(true)] out DocumentSnapshot? document)
    {
        if (updater.TryGetProject(projectKey, out var project))
        {
            return project.TryGetDocument(filePath, out document);
        }

        document = null;
        return false;
    }

    public static DocumentSnapshot? GetDocument(this ProjectSnapshotManager.Updater updater, ProjectKey projectKey, string filePath)
        => updater.TryGetDocument(projectKey, filePath, out var result)
            ? result
            : null;

    public static DocumentSnapshot GetRequiredDocument(this ProjectSnapshotManager.Updater updater, ProjectKey projectKey, string filePath)
        => updater.GetDocument(projectKey, filePath).AssumeNotNull();
}
