// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorCSharpDocument : IRazorGeneratedDocument
{
    public RazorCodeDocument CodeDocument { get; }
    public string GeneratedCode { get; }
    public ImmutableArray<SourceMapping> SourceMappings { get; }
    public ImmutableArray<RazorDiagnostic> Diagnostics { get; }
    public RazorCodeGenerationOptions Options { get; }

    internal IReadOnlyList<LinePragma> LinePragmas { get; }

    public RazorCSharpDocument(
        RazorCodeDocument codeDocument,
        string generatedCode,
        RazorCodeGenerationOptions options,
        ImmutableArray<RazorDiagnostic> diagnostics,
        ImmutableArray<SourceMapping> sourceMappings,
        ImmutableArray<LinePragma> linePragmas)
    {
        ArgHelper.ThrowIfNull(codeDocument);
        ArgHelper.ThrowIfNull(generatedCode);
        ArgHelper.ThrowIfNull(options);

        CodeDocument = codeDocument;
        GeneratedCode = generatedCode;
        Options = options;

        Diagnostics = diagnostics.NullToEmpty();
        SourceMappings = sourceMappings.NullToEmpty();
        LinePragmas = linePragmas.NullToEmpty();
    }

    public static RazorCSharpDocument Create(
        RazorCodeDocument codeDocument,
        string generatedCode,
        RazorCodeGenerationOptions options,
        IEnumerable<RazorDiagnostic> diagnostics)
    {
        ArgHelper.ThrowIfNull(codeDocument);
        ArgHelper.ThrowIfNull(generatedCode);
        ArgHelper.ThrowIfNull(options);
        ArgHelper.ThrowIfNull(diagnostics);

        return new RazorCSharpDocument(codeDocument, generatedCode, options, diagnostics.ToImmutableArray(), sourceMappings: [], linePragmas: default);
    }

    public static RazorCSharpDocument Create(
        RazorCodeDocument codeDocument,
        string generatedCode,
        RazorCodeGenerationOptions options,
        IEnumerable<RazorDiagnostic> diagnostics,
        ImmutableArray<SourceMapping> sourceMappings,
        IEnumerable<LinePragma> linePragmas)
    {
        ArgHelper.ThrowIfNull(codeDocument);
        ArgHelper.ThrowIfNull(generatedCode);
        ArgHelper.ThrowIfNull(options);
        ArgHelper.ThrowIfNull(diagnostics);
        ArgHelper.ThrowIfNull(linePragmas);

        return new RazorCSharpDocument(codeDocument, generatedCode, options, diagnostics.ToImmutableArray(), sourceMappings, linePragmas.ToImmutableArray());
    }
}
