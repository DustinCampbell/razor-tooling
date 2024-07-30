// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorCodeDocument
{
    public RazorSourceDocument Source { get; }
    public ImmutableArray<RazorSourceDocument> Imports { get; }
    public ItemCollection Items { get; }

    public RazorCodeDocument(RazorSourceDocument source)
        : this(source, imports: default, parserOptions: null, codeGenerationOptions: null)
    {
    }

    public RazorCodeDocument(RazorSourceDocument source, ImmutableArray<RazorSourceDocument> imports)
        : this(source, imports, parserOptions: null, codeGenerationOptions: null)
    {
    }

    public RazorCodeDocument(
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> imports,
        RazorParserOptions? parserOptions,
        RazorCodeGenerationOptions? codeGenerationOptions)
    {
        ArgHelper.ThrowIfNull(source);

        Source = source;
        Imports = imports.NullToEmpty();

        Items = [];

        if (parserOptions is not null)
        {
            this.SetParserOptions(parserOptions);
        }

        if (codeGenerationOptions is not null)
        {
            this.SetCodeGenerationOptions(codeGenerationOptions);
        }
    }
}
