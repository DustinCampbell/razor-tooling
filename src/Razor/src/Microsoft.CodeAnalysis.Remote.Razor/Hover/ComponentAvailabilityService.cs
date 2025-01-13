// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Tooltip;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class ComponentAvailabilityService(RemoteRazorSolution solution) : AbstractComponentAvailabilityService
{
    private readonly RemoteRazorSolution _solution = solution;

    protected override ImmutableArray<IRazorProject> GetProjectsContainingDocument(string documentFilePath)
        => _solution.GetProjectsContainingDocument(documentFilePath);
}
