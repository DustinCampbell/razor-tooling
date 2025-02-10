// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorParserOptionsBuilder
{
    private bool _designTime;
    private ImmutableArray<DirectiveDescriptor> _directives;

    public bool DesignTime => _designTime;
    public string FileKind { get; }
    public RazorLanguageVersion LanguageVersion { get; }

    public bool ParseLeadingDirectives { get; set; }
    public bool UseRoslynTokenizer { get; set; }
    public CSharpParseOptions CSharpParseOptions { get; set; }

    internal bool EnableSpanEditHandlers { get; set; }

    internal RazorParserOptionsBuilder(string? fileKind, RazorLanguageVersion version, bool designTime)
    {
        FileKind = fileKind ?? FileKinds.Legacy;
        LanguageVersion = version ?? RazorLanguageVersion.Latest;
        _designTime = designTime;
        CSharpParseOptions = CSharpParseOptions.Default;
    }

    public RazorParserOptions Build()
        => new(_directives, DesignTime, ParseLeadingDirectives, UseRoslynTokenizer, LanguageVersion, FileKind, EnableSpanEditHandlers, CSharpParseOptions);

    public void SetDesignTime(bool designTime)
    {
        _designTime = designTime;
    }

    public void SetDirectives(ImmutableArray<DirectiveDescriptor> directives)
    {
        _directives = directives.NullToEmpty();
    }
}
