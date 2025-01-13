// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.Tooltip;

internal sealed class TestComponentAvailabilityService(RazorSolutionManager solutionManager) : AbstractComponentAvailabilityService
{
    private readonly RazorSolutionManager _solutionManager = solutionManager;

    protected override ImmutableArray<IRazorProject> GetProjectsContainingDocument(string documentFilePath)
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

