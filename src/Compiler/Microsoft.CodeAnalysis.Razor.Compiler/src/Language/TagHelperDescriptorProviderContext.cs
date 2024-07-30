// Licensed to the .NET Foundation under one or more agreements.
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

    public ICollection<TagHelperDescriptor> Results { get; }

    public TagHelperDescriptorProviderContext(Compilation compilation)
        : this(compilation, targetSymbol: null, results: null)
    {
    }

    public TagHelperDescriptorProviderContext(Compilation compilation, ISymbol targetSymbol)
        : this(compilation, targetSymbol, results: null)
    {
    }

    public TagHelperDescriptorProviderContext(Compilation compilation, ICollection<TagHelperDescriptor> results)
        : this(compilation, targetSymbol: null, results)
    {
    }

    public TagHelperDescriptorProviderContext(Compilation compilation, ISymbol? targetSymbol, ICollection<TagHelperDescriptor>? results)
    {
        Compilation = compilation;
        TargetSymbol = targetSymbol;
        Results = results ?? new List<TagHelperDescriptor>();
    }
}
