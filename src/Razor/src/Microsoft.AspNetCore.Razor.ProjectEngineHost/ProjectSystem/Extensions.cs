// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.ProjectSystem;

internal static class Extensions
{
    public static IDocumentSnapshot? GetDocument(this IProjectSnapshot project, string filePath)
        => project.TryGetDocument(filePath, out var result)
            ? result
            : null;

    public static IDocumentSnapshot GetRequiredDocument(this IProjectSnapshot project, string filePath)
        => project.GetDocument(filePath).AssumeNotNull();
}
