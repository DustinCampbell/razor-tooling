// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal partial class DocumentState
{
    internal record struct ImportItem(IDocumentSnapshot Document, VersionStamp Version = default)
    {
        public readonly string? FileKind => Document.FileKind;
        public readonly string? FilePath => Document.FilePath;
    }
}
