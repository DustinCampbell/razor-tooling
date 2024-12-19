// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorCodeDocument
{
    public RazorSourceDocument Source { get; }
    public ImmutableArray<RazorSourceDocument> Imports { get; }
    public RazorFileKind FileKind { get; }
    public ItemCollection Items { get; }

    private RazorCodeDocument(RazorSourceDocument source, ImmutableArray<RazorSourceDocument> imports, RazorFileKind fileKind)
    {
        Source = source;
        Imports = imports.NullToEmpty();
        FileKind = fileKind;

        Items = new ItemCollection();
    }

    public static RazorCodeDocument Create(RazorSourceDocument source, RazorFileKind? fileKind = null)
    {
        ArgHelper.ThrowIfNull(source);

        return new(source, imports: default, fileKind.ToRazorFileKind(source.FilePath));
    }

    public static RazorCodeDocument Create(
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> imports,
        RazorFileKind? fileKind = null)
    {
        ArgHelper.ThrowIfNull(source);

        return new RazorCodeDocument(source, imports, fileKind.ToRazorFileKind(source.FilePath));
    }

    public static RazorCodeDocument Create(
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> imports,
        RazorParserOptions parserOptions,
        RazorCodeGenerationOptions codeGenerationOptions)
    {
        ArgHelper.ThrowIfNull(source);

        var codeDocument = new RazorCodeDocument(source, imports, parserOptions.FileKind);
        codeDocument.SetParserOptions(parserOptions);
        codeDocument.SetCodeGenerationOptions(codeGenerationOptions);
        return codeDocument;
    }
}
