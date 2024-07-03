// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class TagHelperDescriptorProviderContext
{
    public bool ExcludeHidden { get; }
    public bool IncludeDocumentation { get; }

    public ItemCollection Items { get; }
    public ICollection<TagHelperDescriptor> Results { get; }

    private TagHelperDescriptorProviderContext(ICollection<TagHelperDescriptor> results, bool excludeHidden, bool includeDocumentation)
    {
        Results = results;
        ExcludeHidden = excludeHidden;
        IncludeDocumentation = includeDocumentation;

        Items = [];
    }

    public static TagHelperDescriptorProviderContext Create(bool excludeHidden = false, bool includeDocumentation = false)
    {
        return Create(new List<TagHelperDescriptor>(), excludeHidden, includeDocumentation);
    }

    public static TagHelperDescriptorProviderContext Create(ICollection<TagHelperDescriptor> results, bool excludeHidden = false, bool includeDocumentation = false)
    {
        ArgHelper.ThrowIfNull(results);

        return new(results, excludeHidden, includeDocumentation);
    }
}
