// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorParserOptionsBuilder
{
    private bool _designTime;
    private ImmutableArray<DirectiveDescriptor> _directives;

    internal RazorParserOptionsBuilder(RazorConfiguration configuration, string fileKind)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        Configuration = configuration;
        LanguageVersion = configuration.LanguageVersion;
        FileKind = fileKind;
    }

    internal RazorParserOptionsBuilder(bool designTime, RazorLanguageVersion version, string fileKind)
    {
        _designTime = designTime;
        LanguageVersion = version;
        FileKind = fileKind;
    }

    public RazorConfiguration Configuration { get; }

    public bool DesignTime => _designTime;

    public string FileKind { get; }

    public bool ParseLeadingDirectives { get; set; }

    public bool UseRoslynTokenizer { get; set; }

    public CSharpParseOptions CSharpParseOptions { get; set; }

    public RazorLanguageVersion LanguageVersion { get; }

    internal bool EnableSpanEditHandlers { get; set; }

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
