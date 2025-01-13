// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Test;

internal static class RazorSolutionManagerExtensions
{
    public static ISolutionQueryOperations GetQueryOperations(this RazorSolutionManager solutionManager)
        => new TestSolutionQueryOperations(solutionManager);
}

file sealed class TestSolutionQueryOperations(RazorSolutionManager solutionManager) : ISolutionQueryOperations
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
            if (project.ContainsDocument(documentFilePath))
            {
                projects.Add(project);
            }
        }

        return projects.DrainToImmutable();
    }
}
