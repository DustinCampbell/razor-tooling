// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;

internal static partial class IProjectSnapshotManagerExtensions
{
    public static ISolutionQueryOperations GetQueryOperations(this IProjectSnapshotManager projectManager)
        => new SolutionQueryOperations(projectManager);
}
