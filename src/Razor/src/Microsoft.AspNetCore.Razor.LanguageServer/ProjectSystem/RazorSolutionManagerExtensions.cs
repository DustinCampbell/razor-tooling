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

internal static partial class RazorSolutionManagerExtensions
{
    /// <summary>
    /// Finds all the projects where the document path starts with the path of the folder that contains the project file.
    /// </summary>
    public static ImmutableArray<RazorProject> FindPotentialProjects(this RazorSolutionManager solutionManager, string documentFilePath)
    {
        var normalizedDocumentPath = FilePathNormalizer.Normalize(documentFilePath);

        using var projects = new PooledArrayBuilder<RazorProject>();

        foreach (var project in solutionManager.GetProjects())
        {
            // Always exclude the miscellaneous project.
            if (project.FilePath == MiscFilesProject.FilePath)
            {
                continue;
            }

            var projectDirectory = FilePathNormalizer.GetNormalizedDirectoryName(project.FilePath);
            if (normalizedDocumentPath.StartsWith(projectDirectory, FilePathComparison.Instance))
            {
                projects.Add(project);
            }
        }

        return projects.DrainToImmutable();
    }

    public static bool TryResolveAllProjects(
        this RazorSolutionManager solutionManager,
        string documentFilePath,
        out ImmutableArray<RazorProject> projects)
    {
        var potentialProjects = solutionManager.FindPotentialProjects(documentFilePath);

        using var builder = new PooledArrayBuilder<RazorProject>(capacity: potentialProjects.Length);

        foreach (var project in potentialProjects)
        {
            if (project.ContainsDocument(documentFilePath))
            {
                builder.Add(project);
            }
        }

        var normalizedDocumentPath = FilePathNormalizer.Normalize(documentFilePath);
        var miscProject = solutionManager.GetMiscellaneousProject();
        if (miscProject.ContainsDocument(normalizedDocumentPath))
        {
            builder.Add(miscProject);
        }

        projects = builder.DrainToImmutable();
        return projects.Length > 0;
    }

    public static bool TryResolveDocumentInAnyProject(
        this RazorSolutionManager solutionManager,
        string documentFilePath,
        ILogger logger,
        [NotNullWhen(true)] out RazorDocument? document)
    {
        logger.LogTrace($"Looking for {documentFilePath}.");

        var normalizedDocumentPath = FilePathNormalizer.Normalize(documentFilePath);
        var potentialProjects = solutionManager.FindPotentialProjects(documentFilePath);

        foreach (var project in potentialProjects)
        {
            if (project.TryGetDocument(normalizedDocumentPath, out document))
            {
                logger.LogTrace($"Found {documentFilePath} in {project.FilePath}");
                return true;
            }
        }

        logger.LogTrace($"Looking for {documentFilePath} in miscellaneous project.");
        var miscellaneousProject = solutionManager.GetMiscellaneousProject();

        if (miscellaneousProject.TryGetDocument(normalizedDocumentPath, out document))
        {
            logger.LogTrace($"Found {documentFilePath} in miscellaneous project.");
            return true;
        }

        logger.LogTrace($"{documentFilePath} not found in {string.Join(", ", solutionManager.GetProjects().SelectMany(p => p.DocumentFilePaths))}");

        document = null;
        return false;
    }

    public static ISolutionQueryOperations GetQueryOperations(this RazorSolutionManager solutionManager)
        => new SolutionQueryOperations(solutionManager);
}
