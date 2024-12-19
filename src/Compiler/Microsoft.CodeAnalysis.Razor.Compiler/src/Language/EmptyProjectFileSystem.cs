// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

internal class EmptyProjectFileSystem : RazorProjectFileSystem
{
    public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
    {
        NormalizeAndEnsureValidPath(basePath);
        return Enumerable.Empty<RazorProjectItem>();
    }

    public override RazorProjectItem GetItem(string path, RazorFileKind? fileKind = null)
    {
        NormalizeAndEnsureValidPath(path);
        return new NotFoundProjectItem(string.Empty, path, fileKind.ToRazorFileKind(path));
    }
}
