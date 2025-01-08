// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

public static class TestRazorCodeDocument
{
    public static RazorCodeDocument CreateEmpty()
    {
        var source = TestRazorSourceDocument.CreateEmpty();
        return RazorCodeDocument.Create(source, imports: default);
    }

    public static RazorCodeDocument Create(string content, bool normalizeNewLines = false)
    {
        var source = TestRazorSourceDocument.Create(content, normalizeNewLines: normalizeNewLines);
        return RazorCodeDocument.Create(source, imports: default);
    }

    public static RazorCodeDocument Create(
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> imports = default,
        Action<RazorParserOptionsBuilder>? configureParserOptions = null,
        Action<RazorCodeGenerationOptionsBuilder>? configureCodeGenerationOptions = null)
    {
        var parserOptions = configureParserOptions is not null
            ? RazorParserOptions.Create(configureParserOptions)
            : RazorParserOptions.CreateDefault();

        var codeGenerationOptions = configureCodeGenerationOptions is not null
            ? RazorCodeGenerationOptions.Create(configureCodeGenerationOptions)
            : RazorCodeGenerationOptions.Default;

        return RazorCodeDocument.Create(source, imports, parserOptions, codeGenerationOptions);
    }
}
