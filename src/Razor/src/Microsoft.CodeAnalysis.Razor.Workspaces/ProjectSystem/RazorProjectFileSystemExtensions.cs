// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal static class RazorProjectFileSystemExtensions
{
    public static RazorProjectItem GetProjectItem(this RazorProjectFileSystem fileSystem, IDocumentSnapshot document)
        => fileSystem.GetItem(document.FilePath, document.FileKind);

    public static RazorProjectItem GetProjectItem(this RazorProjectFileSystem fileSystem, HostDocument hostDocument)
        => fileSystem.GetItem(hostDocument.FilePath, hostDocument.FileKind);
}
