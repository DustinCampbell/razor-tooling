// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorCodeDocument
{
    public RazorSourceDocument Source { get; }
    public ImmutableArray<RazorSourceDocument> Imports { get; }
    public ItemCollection Items { get; }

    private readonly RazorParserOptions? _parserOptions;
    private readonly RazorCodeGenerationOptions? _codeGenerationOptions;

    private RazorCodeDocument(
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> imports,
        RazorParserOptions? parserOptions = null,
        RazorCodeGenerationOptions? codeGenerationOptions = null)
    {
        Source = source;
        Imports = imports.NullToEmpty();

        _parserOptions = parserOptions;
        _codeGenerationOptions = codeGenerationOptions;

        Items = new ItemCollection();
    }

    public static RazorCodeDocument Create(RazorSourceDocument source)
    {
        ArgHelper.ThrowIfNull(source);

        return Create(source, imports: default);
    }

    public static RazorCodeDocument Create(
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> imports)
    {
        ArgHelper.ThrowIfNull(source);

        return new RazorCodeDocument(source, imports);
    }

    public static RazorCodeDocument Create(
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> imports,
        RazorParserOptions? parserOptions,
        RazorCodeGenerationOptions? codeGenerationOptions)
    {
        ArgHelper.ThrowIfNull(source);

        return new RazorCodeDocument(source, imports, parserOptions, codeGenerationOptions);
    }

    public RazorParserOptions GetRequiredParserOptions()
        => GetParserOptions().AssumeNotNull();

    public RazorParserOptions? GetParserOptions()
        => _parserOptions;

    public RazorCodeGenerationOptions GetRequiredCodeGenerationOptions()
        => GetCodeGenerationOptions().AssumeNotNull();

    public RazorCodeGenerationOptions? GetCodeGenerationOptions()
        => _codeGenerationOptions;
}
