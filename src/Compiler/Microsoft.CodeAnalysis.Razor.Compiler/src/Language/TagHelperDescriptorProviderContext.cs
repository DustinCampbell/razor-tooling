// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class TagHelperDescriptorProviderContext
{
    public Compilation Compilation { get; }
    public ISymbol? TargetSymbol { get; }
    public bool ExcludeHidden { get; }
    public bool IncludeDocumentation { get; }

    public ICollection<TagHelperDescriptor> Results { get; }

    private TagHelperDescriptorProviderContext(
        Compilation compilation,
        ISymbol? targetSymbol,
        ICollection<TagHelperDescriptor> results,
        bool excludeHidden,
        bool includeDocumentation)
    {
        Compilation = compilation;
        TargetSymbol = targetSymbol;
        Results = results;
        ExcludeHidden = excludeHidden;
        IncludeDocumentation = includeDocumentation;
    }

    public static TagHelperDescriptorProviderContext Create(
        Compilation compilation,
        ISymbol? targetSymbol = null,
        bool excludeHidden = false,
        bool includeDocumentation = false)
    {
        return Create(compilation, targetSymbol, new List<TagHelperDescriptor>(), excludeHidden, includeDocumentation);
    }

    public static TagHelperDescriptorProviderContext Create(
        Compilation compilation,
        ISymbol? targetSymbol,
        ICollection<TagHelperDescriptor> results,
        bool excludeHidden = false,
        bool includeDocumentation = false)
    {
        ArgHelper.ThrowIfNull(results);

        return new(compilation, targetSymbol, results, excludeHidden, includeDocumentation);
    }
}
