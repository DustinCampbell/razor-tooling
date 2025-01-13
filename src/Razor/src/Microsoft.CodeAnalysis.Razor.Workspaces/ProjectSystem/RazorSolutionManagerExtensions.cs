// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal static class RazorSolutionManagerExtensions
{
    public static RazorProject? GetProject(this RazorSolutionManager solutionManager, ProjectKey projectKey)
        => solutionManager.TryGetProject(projectKey, out var result)
            ? result
            : null;

    public static RazorProject GetRequiredProject(this RazorSolutionManager solutionManager, ProjectKey projectKey)
        => solutionManager.GetProject(projectKey).AssumeNotNull();

    public static bool ContainsDocument(this RazorSolutionManager solutionManager, ProjectKey projectKey, string documentFilePath)
        => solutionManager.TryGetProject(projectKey, out var project) &&
           project.ContainsDocument(documentFilePath);

    public static bool TryGetDocument(
        this RazorSolutionManager solutionManager,
        ProjectKey projectKey,
        string documentFilePath,
        [NotNullWhen(true)] out RazorDocument? document)
    {
        document = solutionManager.TryGetProject(projectKey, out var project)
            ? project.GetDocument(documentFilePath)
            : null;

        return document is not null;
    }

    public static RazorDocument? GetDocument(this RazorSolutionManager solutionManager, ProjectKey projectKey, string documentFilePath)
        => solutionManager.TryGetDocument(projectKey, documentFilePath, out var result)
            ? result
            : null;

    public static RazorDocument GetRequiredDocument(this RazorSolutionManager solutionManager, ProjectKey projectKey, string documentFilePath)
        => solutionManager.GetDocument(projectKey, documentFilePath).AssumeNotNull();

    public static RazorProject? GetProject(this RazorSolutionManager.Updater updater, ProjectKey projectKey)
        => updater.TryGetProject(projectKey, out var result)
            ? result
            : null;

    public static RazorProject GetRequiredProject(this RazorSolutionManager.Updater updater, ProjectKey projectKey)
        => updater.GetProject(projectKey).AssumeNotNull();

    public static bool ContainsDocument(this RazorSolutionManager.Updater updater, ProjectKey projectKey, string documentFilePath)
        => updater.TryGetProject(projectKey, out var project) &&
           project.ContainsDocument(documentFilePath);

    public static bool TryGetDocument(
        this RazorSolutionManager.Updater updater,
        ProjectKey projectKey,
        string documentFilePath,
        [NotNullWhen(true)] out RazorDocument? document)
    {
        document = updater.TryGetProject(projectKey, out var project)
            ? project.GetDocument(documentFilePath)
            : null;

        return document is not null;
    }

    public static RazorDocument? GetDocument(this RazorSolutionManager.Updater updater, ProjectKey projectKey, string documentFilePath)
        => updater.TryGetDocument(projectKey, documentFilePath, out var result)
            ? result
            : null;

    public static RazorDocument GetRequiredDocument(this RazorSolutionManager.Updater updater, ProjectKey projectKey, string documentFilePath)
        => updater.GetDocument(projectKey, documentFilePath).AssumeNotNull();
}
