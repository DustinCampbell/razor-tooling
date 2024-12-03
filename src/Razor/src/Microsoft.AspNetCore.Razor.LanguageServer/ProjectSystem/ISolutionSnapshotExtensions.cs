// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;

internal static class ISolutionSnapshotExtensions
{
    public static IProjectSnapshot GetMiscellaneousProject(this ISolutionSnapshot solution)
    {
        return solution.GetRequiredProject(MiscFilesHostProject.Instance.Key);
    }

    /// <summary>
    /// Finds all the projects where the document path starts with the path of the folder that contains the project file.
    /// </summary>
    public static ImmutableArray<IProjectSnapshot> FindPotentialProjects(this ISolutionSnapshot solution, string documentFilePath)
    {
        var normalizedDocumentPath = FilePathNormalizer.Normalize(documentFilePath);

        using var projects = new PooledArrayBuilder<IProjectSnapshot>();

        foreach (var project in solution.Projects)
        {
            // Always exclude the miscellaneous project.
            if (project.Key == MiscFilesHostProject.Instance.Key)
            {
                continue;
            }

            var projectDirectory = FilePathNormalizer.GetNormalizedDirectoryName(project.FilePath);
            if (normalizedDocumentPath.StartsWith(projectDirectory, FilePathComparison.Instance))
            {
                projects.Add(project);
            }
        }

        return projects.DrainToImmutableOrderedBy(static x => x.Key);
    }

    public static bool TryResolveAllProjects(
        this ISolutionSnapshot solution,
        string documentFilePath,
        out ImmutableArray<IProjectSnapshot> projects)
    {
        var potentialProjects = solution.FindPotentialProjects(documentFilePath);

        using var builder = new PooledArrayBuilder<IProjectSnapshot>(capacity: potentialProjects.Length);

        foreach (var project in potentialProjects)
        {
            if (project.ContainsDocument(documentFilePath))
            {
                builder.Add(project);
            }
        }

        var normalizedDocumentPath = FilePathNormalizer.Normalize(documentFilePath);
        var miscProject = solution.GetMiscellaneousProject();
        if (miscProject.ContainsDocument(normalizedDocumentPath))
        {
            builder.Add(miscProject);
        }

        projects = builder.DrainToImmutable();
        return projects.Length > 0;
    }

    public static bool TryResolveDocumentInAnyProject(
        this ISolutionSnapshot solution,
        string documentFilePath,
        ILogger logger,
        [NotNullWhen(true)] out IDocumentSnapshot? document)
    {
        logger.LogTrace($"Looking for {documentFilePath}.");

        var normalizedDocumentPath = FilePathNormalizer.Normalize(documentFilePath);

        var potentialProjects = solution.FindPotentialProjects(documentFilePath);

        foreach (var project in potentialProjects)
        {
            if (project.TryGetDocument(normalizedDocumentPath, out document))
            {
                logger.LogTrace($"Found {documentFilePath} in {project.FilePath}");
                return true;
            }
        }

        logger.LogTrace($"Looking for {documentFilePath} in miscellaneous project.");
        var miscellaneousProject = solution.GetMiscellaneousProject();

        if (miscellaneousProject.TryGetDocument(normalizedDocumentPath, out document))
        {
            logger.LogTrace($"Found {documentFilePath} in miscellaneous project.");
            return true;
        }

        logger.LogTrace($"{documentFilePath} not found in {string.Join(", ", solution.Projects.SelectMany(static p => p.DocumentFilePaths))}");

        document = null;
        return false;
    }
}
