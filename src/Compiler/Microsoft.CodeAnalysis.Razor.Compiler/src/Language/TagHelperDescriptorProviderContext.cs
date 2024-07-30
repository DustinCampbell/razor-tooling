﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class TagHelperDescriptorProviderContext
{
    public Compilation Compilation { get; }
    public ISymbol? TargetSymbol { get; }
    public bool ExcludeHidden { get; set; }
    public bool IncludeDocumentation { get; set; }

    public ItemCollection Items { get; }
    public ICollection<TagHelperDescriptor> Results { get; }

    private TagHelperDescriptorProviderContext(Compilation compilation, ISymbol? targetSymbol, ICollection<TagHelperDescriptor> results)
    {
        Compilation = compilation;
        TargetSymbol = targetSymbol;
        Results = results;
        Items = [];
    }

    public static TagHelperDescriptorProviderContext Create(Compilation compilation, ISymbol? targetSymbol = null)
    {
        return new(compilation, targetSymbol, new List<TagHelperDescriptor>());
    }

    public static TagHelperDescriptorProviderContext Create(Compilation compilation, ISymbol? targetSymbol, ICollection<TagHelperDescriptor> results)
    {
        ArgHelper.ThrowIfNull(results);

        return new(compilation, targetSymbol, results);
    }
}
