// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorCSharpDocument : IRazorGeneratedDocument
{
    public RazorCodeDocument CodeDocument { get; }
    public SourceText GeneratedText { get; }
    public RazorCodeGenerationOptions Options { get; }
    public ImmutableArray<RazorDiagnostic> Diagnostics { get; }
    public ImmutableArray<SourceMapping> SourceMappings { get; }
    public ImmutableArray<LinePragma> LinePragmas { get; }

    private string? _generatedCode;

    public RazorCSharpDocument(
        RazorCodeDocument codeDocument,
        SourceText generatedText,
        RazorCodeGenerationOptions options,
        ImmutableArray<RazorDiagnostic> diagnostics,
        ImmutableArray<SourceMapping> sourceMappings,
        ImmutableArray<LinePragma> linePragmas)
    {
        ArgHelper.ThrowIfNull(codeDocument);
        ArgHelper.ThrowIfNull(generatedText);
        ArgHelper.ThrowIfNull(options);

        CodeDocument = codeDocument;
        GeneratedText = generatedText;
        Options = options;

        Diagnostics = diagnostics.NullToEmpty();
        SourceMappings = sourceMappings.NullToEmpty();
        LinePragmas = linePragmas.NullToEmpty();
    }

    public string GeneratedCode => _generatedCode ??= GeneratedText.ToString();

    public static RazorCSharpDocument Create(RazorCodeDocument codeDocument, string generatedCode, RazorCodeGenerationOptions options, IEnumerable<RazorDiagnostic> diagnostics)
        => new(codeDocument, SourceText.From(generatedCode), options, diagnostics.ToImmutableArray(), sourceMappings: [], linePragmas: []);

    public static RazorCSharpDocument Create(
        RazorCodeDocument codeDocument,
        string generatedCode,
        RazorCodeGenerationOptions options,
        IEnumerable<RazorDiagnostic> diagnostics,
        ImmutableArray<SourceMapping> sourceMappings,
        IEnumerable<LinePragma> linePragmas)
        => new(codeDocument, SourceText.From(generatedCode), options, diagnostics.ToImmutableArray(), sourceMappings, linePragmas.ToImmutableArray());
}
