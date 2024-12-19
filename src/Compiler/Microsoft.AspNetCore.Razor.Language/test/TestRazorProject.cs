// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

public class TestRazorProject(params IEnumerable<RazorProjectItem> items) : RazorProject
{
    private readonly Dictionary<string, RazorProjectItem> _lookup = items.ToDictionary(item => item.FilePath);

    public TestRazorProject()
        : this([])
    {
    }

    public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
    {
        throw new NotImplementedException();
    }

    public override RazorProjectItem GetItem(string path, RazorFileKind? fileKind = null)
    {
        if (_lookup.TryGetValue(path, out var projectItem))
        {
            return projectItem;
        }

        return new NotFoundProjectItem("", path, fileKind.ToRazorFileKind(path));
    }

    public new string NormalizeAndEnsureValidPath(string path)
        => base.NormalizeAndEnsureValidPath(path);
}
