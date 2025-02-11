// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorParserOptionsBuilder
{
    private RazorParserOptionsFlags _flags;

    private ImmutableArray<DirectiveDescriptor> _directives;

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

    internal bool AllowMinimizedBooleanTagHelperAttributes
    {
        get => _flags.IsFlagSet(RazorParserOptionsFlags.AllowMinimizedBooleanTagHelperAttributes);
        set => _flags.UpdateFlag(RazorParserOptionsFlags.AllowMinimizedBooleanTagHelperAttributes, value);
    }

    internal bool AllowHtmlCommentsInTagHelpers
    {
        get => _flags.IsFlagSet(RazorParserOptionsFlags.AllowHtmlCommentsInTagHelpers);
        set => _flags.UpdateFlag(RazorParserOptionsFlags.AllowHtmlCommentsInTagHelpers, value);
    }

    internal bool AllowComponentFileKind
    {
        get => _flags.IsFlagSet(RazorParserOptionsFlags.AllowComponentFileKind);
        set => _flags.UpdateFlag(RazorParserOptionsFlags.AllowComponentFileKind, value);
    }

    internal bool AllowRazorInAllCodeBlocks
    {
        get => _flags.IsFlagSet(RazorParserOptionsFlags.AllowRazorInAllCodeBlocks);
        set => _flags.UpdateFlag(RazorParserOptionsFlags.AllowRazorInAllCodeBlocks, value);
    }

    internal bool AllowUsingVariableDeclarations
    {
        get => _flags.IsFlagSet(RazorParserOptionsFlags.AllowUsingVariableDeclarations);
        set => _flags.UpdateFlag(RazorParserOptionsFlags.AllowUsingVariableDeclarations, value);
    }

    internal bool AllowConditionalDataDashAttributes
    {
        get => _flags.IsFlagSet(RazorParserOptionsFlags.AllowConditionalDataDashAttributes);
        set => _flags.UpdateFlag(RazorParserOptionsFlags.AllowConditionalDataDashAttributes, value);
    }

    internal bool AllowCSharpInMarkupAttributeArea
    {
        get => _flags.IsFlagSet(RazorParserOptionsFlags.AllowCSharpInMarkupAttributeArea);
        set => _flags.UpdateFlag(RazorParserOptionsFlags.AllowCSharpInMarkupAttributeArea, value);
    }

    internal bool AllowNullableForgivenessOperator
    {
        get => _flags.IsFlagSet(RazorParserOptionsFlags.AllowNullableForgivenessOperator);
        set => _flags.UpdateFlag(RazorParserOptionsFlags.AllowNullableForgivenessOperator, value);
    }

    internal RazorParserOptionsBuilder(string? fileKind, RazorLanguageVersion version, bool designTime)
    {
        FileKind = fileKind ?? FileKinds.Legacy;
        LanguageVersion = version ?? RazorLanguageVersion.Latest;
        CSharpParseOptions = CSharpParseOptions.Default;

        _flags = ComputeFlags(FileKind, LanguageVersion, designTime);
    }

    private static RazorParserOptionsFlags ComputeFlags(string fileKind, RazorLanguageVersion version, bool designTime)
    {
        RazorParserOptionsFlags flags = 0;

        if (designTime)
        {
            flags.SetFlag(RazorParserOptionsFlags.DesignTime);
        }

        flags.SetFlag(RazorParserOptionsFlags.AllowCSharpInMarkupAttributeArea);

        if (version >= RazorLanguageVersion.Version_2_1)
        {
            // Added in 2.1
            flags.SetFlag(RazorParserOptionsFlags.AllowMinimizedBooleanTagHelperAttributes);
            flags.SetFlag(RazorParserOptionsFlags.AllowHtmlCommentsInTagHelpers);
        }

        if (version >= RazorLanguageVersion.Version_3_0)
        {
            // Added in 3.0
            flags.SetFlag(RazorParserOptionsFlags.AllowComponentFileKind);
            flags.SetFlag(RazorParserOptionsFlags.AllowRazorInAllCodeBlocks);
            flags.SetFlag(RazorParserOptionsFlags.AllowUsingVariableDeclarations);
            flags.SetFlag(RazorParserOptionsFlags.AllowNullableForgivenessOperator);
        }

        if (FileKinds.IsComponent(fileKind))
        {
            flags.SetFlag(RazorParserOptionsFlags.AllowConditionalDataDashAttributes);
            flags.ClearFlag(RazorParserOptionsFlags.AllowCSharpInMarkupAttributeArea);
        }

        if (version >= RazorLanguageVersion.Experimental)
        {
            flags.SetFlag(RazorParserOptionsFlags.AllowConditionalDataDashAttributes);
        }

        return flags;
    }

    public RazorParserOptions Build()
        => new(
            _flags,
            _directives,
            LanguageVersion,
            FileKind,
            CSharpParseOptions);

    public void SetDesignTime(bool value)
    {
        _flags.UpdateFlag(RazorParserOptionsFlags.DesignTime, value);
    }

    public void SetDirectives(ImmutableArray<DirectiveDescriptor> directives)
    {
        _directives = directives.NullToEmpty();
    }
}
