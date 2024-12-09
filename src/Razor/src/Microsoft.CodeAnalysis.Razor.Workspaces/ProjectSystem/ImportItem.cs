// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal readonly record struct ImportItem(RazorProjectItem ProjectItem, TextAndVersion TextAndVersion)
{
    public bool IsDefault => ProjectItem.PhysicalPath is null;

    public string FilePath => ProjectItem.FilePath;
    public string? RelativePath => ProjectItem.RelativePhysicalPath;

    public SourceText Text => TextAndVersion.Text;
    public VersionStamp Version => TextAndVersion.Version;

    public RazorSourceDocument CreateSourceDocument()
        => RazorSourceDocument.Create(Text, RazorSourceDocumentProperties.Create(FilePath, RelativePath));
}
