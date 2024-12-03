// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;

internal static class ISolutionSnapshotExtensions
{
    public static IProjectSnapshot GetMiscellaneousProject(this ISolutionSnapshot solution)
    {
        return solution.GetRequiredProject(MiscFilesHostProject.Instance.Key);
    }

}
