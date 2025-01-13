// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;

internal static partial class RazorSolutionManagerExtensions
{
    private sealed class SolutionQueryOperations(RazorSolutionManager solutionManager) : ISolutionQueryOperations
    {
        private readonly RazorSolutionManager _solutionManager = solutionManager;

        public IEnumerable<IRazorProject> GetProjects()
        {
            return _solutionManager.GetProjects().Cast<IRazorProject>();
        }

        public ImmutableArray<IRazorProject> GetProjectsContainingDocument(string documentFilePath)
        {
            using var projects = new PooledArrayBuilder<IRazorProject>();

            foreach (var project in _solutionManager.GetProjects())
            {
                // Always exclude the miscellaneous project.
                if (project.Key == MiscFilesProject.Key)
                {
                    continue;
                }

                if (project.ContainsDocument(documentFilePath))
                {
                    projects.Add(project);
                }
            }

            return projects.DrainToImmutable();
        }
    }
}
