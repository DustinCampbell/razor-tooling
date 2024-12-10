// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal static class ProjectSnapshotExtensions
{
    public static DocumentSnapshot? GetDocument(this ProjectSnapshot project, string filePath)
        => project.TryGetDocument(filePath, out var result)
            ? result
            : null;

    public static DocumentSnapshot GetRequiredDocument(this ProjectSnapshot project, string filePath)
        => project.GetDocument(filePath).AssumeNotNull();
}
