// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public class TestTagHelperFeature : RazorEngineFeatureBase, ITagHelperFeature
{
    private TagHelperDescriptorCollection _tagHelpers;

    public TestTagHelperFeature()
    {
        _tagHelpers = [];
    }

    public TestTagHelperFeature(IEnumerable<TagHelperDescriptor> tagHelpers)
    {
        _tagHelpers = [.. tagHelpers];
    }

    public void AddTagHelpers(IEnumerable<TagHelperDescriptor> tagHelpers)
    {
        _tagHelpers = TagHelperDescriptorCollection.Merge(_tagHelpers, [.. tagHelpers]);
    }

    public TagHelperDescriptorCollection GetDescriptors()
        => _tagHelpers;
}
