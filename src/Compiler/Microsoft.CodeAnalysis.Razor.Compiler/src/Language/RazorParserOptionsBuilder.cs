// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorParserOptionsBuilder
{
    private bool _designTime;

    internal RazorParserOptionsBuilder(RazorConfiguration configuration, RazorFileKind? fileKind)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        Configuration = configuration;
        LanguageVersion = configuration.LanguageVersion;
        FileKind = fileKind;
    }

    internal RazorParserOptionsBuilder(bool designTime, RazorLanguageVersion version, RazorFileKind? fileKind)
    {
        _designTime = designTime;
        LanguageVersion = version;
        FileKind = fileKind;
    }

    public RazorConfiguration Configuration { get; }

    public bool DesignTime => _designTime;

    public ICollection<DirectiveDescriptor> Directives { get; } = new List<DirectiveDescriptor>();

    public RazorFileKind? FileKind { get; }

    public bool ParseLeadingDirectives { get; set; }

    public bool UseRoslynTokenizer { get; set; }

    public CSharpParseOptions CSharpParseOptions { get; set; }

    public RazorLanguageVersion LanguageVersion { get; }

    internal bool EnableSpanEditHandlers { get; set; }

    public RazorParserOptions Build()
    {
        return new RazorParserOptions(
            Directives.ToArray(), DesignTime, ParseLeadingDirectives, UseRoslynTokenizer, LanguageVersion, FileKind ?? RazorFileKind.Legacy, EnableSpanEditHandlers, CSharpParseOptions);
    }

    public void SetDesignTime(bool designTime)
    {
        _designTime = designTime;
    }
}
