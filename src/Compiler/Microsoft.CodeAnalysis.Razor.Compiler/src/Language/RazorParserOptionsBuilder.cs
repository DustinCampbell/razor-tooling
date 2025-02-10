// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorParserOptionsBuilder
{
    private RazorParserOptionsFlags _flags;

    private ImmutableArray<DirectiveDescriptor> _directives;
    private RazorParserFeatureFlags? _featureFlags;

    public bool DesignTime => _flags.IsFlagSet(RazorParserOptionsFlags.DesignTime);
    public string FileKind { get; }
    public RazorLanguageVersion LanguageVersion { get; }
    public CSharpParseOptions CSharpParseOptions { get; set; }

    public bool ParseLeadingDirectives
    {
        get => _flags.IsFlagSet(RazorParserOptionsFlags.ParseLeadingDirectives);
        set => _flags.UpdateFlag(RazorParserOptionsFlags.ParseLeadingDirectives, value);
    }

    public bool UseRoslynTokenizer
    {
        get => _flags.IsFlagSet(RazorParserOptionsFlags.UseRoslynTokenizer);
        set => _flags.UpdateFlag(RazorParserOptionsFlags.UseRoslynTokenizer, value);
    }

    internal bool EnableSpanEditHandlers
    {
        get => _flags.IsFlagSet(RazorParserOptionsFlags.EnableSpanEditHandlers);
        set => _flags.UpdateFlag(RazorParserOptionsFlags.EnableSpanEditHandlers, value);
    }

    internal RazorParserOptionsBuilder(string? fileKind, RazorLanguageVersion version, bool designTime)
    {
        FileKind = fileKind ?? FileKinds.Legacy;
        LanguageVersion = version ?? RazorLanguageVersion.Latest;
        CSharpParseOptions = CSharpParseOptions.Default;

        if (designTime)
        {
            _flags = RazorParserOptionsFlags.DesignTime;
        }
    }

    public RazorParserOptions Build()
        => new(
            _flags,
            _directives,
            LanguageVersion,
            FileKind,
            CSharpParseOptions)
        {
            FeatureFlags = _featureFlags ?? RazorParserFeatureFlags.Create(LanguageVersion, FileKind)
        };

    public void SetDesignTime(bool value)
    {
        _flags.UpdateFlag(RazorParserOptionsFlags.DesignTime, value);
    }

    public void SetDirectives(ImmutableArray<DirectiveDescriptor> directives)
    {
        _directives = directives.NullToEmpty();
    }

    internal void SetFeatureFlags(RazorParserFeatureFlags featureFlags)
    {
        _featureFlags = featureFlags;
    }
}
