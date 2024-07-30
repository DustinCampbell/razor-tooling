// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorParserOptionsBuilder
{
    private bool _designTime;

    public bool DesignTime => _designTime;

    public ImmutableArray<DirectiveDescriptor>.Builder Directives { get; }
    public string? FileKind { get; }
    public bool ParseLeadingDirectives { get; set; }
    public RazorLanguageVersion LanguageVersion { get; }
    internal bool EnableSpanEditHandlers { get; set; }

    internal RazorParserOptionsBuilder(RazorConfiguration configuration, string? fileKind)
    {
        ArgHelper.ThrowIfNull(configuration);

        LanguageVersion = configuration.LanguageVersion;
        FileKind = fileKind;
        Directives = ImmutableArray.CreateBuilder<DirectiveDescriptor>();
    }

    internal RazorParserOptionsBuilder(bool designTime, RazorLanguageVersion version, string? fileKind)
    {
        _designTime = designTime;
        LanguageVersion = version;
        FileKind = fileKind;
        Directives = ImmutableArray.CreateBuilder<DirectiveDescriptor>();
    }

    public RazorParserOptions Build()
        => new(
            Directives.ToImmutable(),
            DesignTime,
            ParseLeadingDirectives,
            LanguageVersion,
            FileKind ?? FileKinds.Legacy,
            EnableSpanEditHandlers);

    public void SetDesignTime(bool designTime)
    {
        _designTime = designTime;
    }
}
