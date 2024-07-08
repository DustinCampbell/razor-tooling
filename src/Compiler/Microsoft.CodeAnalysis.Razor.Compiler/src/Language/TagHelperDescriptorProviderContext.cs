// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class TagHelperDescriptorProviderContext : IDisposable
{
    public Compilation Compilation { get; }
    public ISymbol? TargetSymbol { get; }
    public bool ExcludeHidden { get; }
    public bool IncludeDocumentation { get; }

    public TagHelperDescriptorCollection.IBuilder Results { get; }

    private TagHelperDescriptorProviderContext(
        Compilation compilation,
        ISymbol? targetSymbol,
        TagHelperDescriptorCollection.IBuilder results,
        bool excludeHidden,
        bool includeDocumentation)
    {
        Compilation = compilation;
        TargetSymbol = targetSymbol;
        Results = results;
        ExcludeHidden = excludeHidden;
        IncludeDocumentation = includeDocumentation;
    }

    public void Dispose()
    {
        Results.Dispose();
    }

    public static TagHelperDescriptorProviderContext Create(
        Compilation compilation,
        ISymbol? targetSymbol = null,
        bool excludeHidden = false,
        bool includeDocumentation = false)
    {
        return Create(compilation, targetSymbol, TagHelperDescriptorCollection.GetBuilder(), excludeHidden, includeDocumentation);
    }

    public static TagHelperDescriptorProviderContext Create(
        Compilation compilation,
        ISymbol? targetSymbol,
        TagHelperDescriptorCollection.IBuilder results,
        bool excludeHidden = false,
        bool includeDocumentation = false)
    {
        ArgHelper.ThrowIfNull(results);

        return new(compilation, targetSymbol, results, excludeHidden, includeDocumentation);
    }
}
