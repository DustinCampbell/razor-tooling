// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem.Sources;

internal sealed class SourceAndVersion
{
    public RazorSourceDocument Source { get; }
    public VersionStamp Version { get; }

    private SourceAndVersion(RazorSourceDocument source, VersionStamp version)
    {
        Source = source;
        Version = version;
    }

    public static SourceAndVersion Create(RazorSourceDocument source, VersionStamp version)
        => new(source, version);
}
