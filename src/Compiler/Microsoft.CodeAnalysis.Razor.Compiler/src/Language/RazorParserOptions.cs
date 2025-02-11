// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial record class RazorParserOptions
{
    private static RazorLanguageVersion DefaultLanguageVersion => RazorLanguageVersion.Latest;
    private static string DefaultFileKind => FileKinds.Legacy;
    private static CSharpParseOptions DefaultCSharpParseOptions => CSharpParseOptions.Default;

    public static RazorParserOptions Default { get; } = new(
        languageVersion: DefaultLanguageVersion,
        fileKind: DefaultFileKind,
        directives: [],
        csharpParseOptions: DefaultCSharpParseOptions,
        flags: GetDefaultFlags(DefaultLanguageVersion, DefaultFileKind));

    private readonly RazorParserOptionsFlags _flags;

    public RazorLanguageVersion LanguageVersion { get; }
    public string FileKind { get; }
    public ImmutableArray<DirectiveDescriptor> Directives { get; }
    public CSharpParseOptions CSharpParseOptions { get; }

    internal RazorParserOptions(
        RazorLanguageVersion languageVersion,
        string fileKind,
        ImmutableArray<DirectiveDescriptor> directives,
        CSharpParseOptions csharpParseOptions,
        RazorParserOptionsFlags flags)
    {
        if (flags.IsFlagSet(RazorParserOptionsFlags.ParseLeadingDirectives) &&
            flags.IsFlagSet(RazorParserOptionsFlags.UseRoslynTokenizer))
        {
            ThrowHelper.ThrowInvalidOperationException($"{nameof(RazorParserOptionsFlags.ParseLeadingDirectives)} and {nameof(RazorParserOptionsFlags.UseRoslynTokenizer)} can't both be true.");
        }

        Directives = directives.NullToEmpty();
        LanguageVersion = languageVersion ?? DefaultLanguageVersion;
        FileKind = fileKind ?? DefaultFileKind;
        CSharpParseOptions = csharpParseOptions ?? DefaultCSharpParseOptions;
        _flags = flags;
    }

    /// <summary>
    ///  Creates a new instance of <see cref="RazorParserOptions"/> with the specified language version and file kind.
    /// </summary>
    public static RazorParserOptions Create(RazorLanguageVersion languageVersion, string fileKind)
    {
        fileKind ??= DefaultFileKind;

        if (languageVersion == DefaultLanguageVersion &&
            StringComparer.OrdinalIgnoreCase.Equals(fileKind, DefaultFileKind))
        {
            return Default;
        }

        return new(
            languageVersion,
            fileKind,
            directives: [],
            csharpParseOptions: DefaultCSharpParseOptions,
            flags: GetDefaultFlags(languageVersion, fileKind));
    }

    public bool Equals(RazorParserOptions? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null &&
               _flags == other._flags &&
               LanguageVersion.Equals(other.LanguageVersion) &&
               StringComparer.OrdinalIgnoreCase.Equals(FileKind, other.FileKind) &&
               Directives.SequenceEqual(other.Directives) &&
               CSharpParseOptions.Equals(other.CSharpParseOptions);
    }

    public override int GetHashCode()
    {
        var hash = HashCodeCombiner.Start();

        hash.Add(_flags);
        hash.Add(LanguageVersion);
        hash.Add(FileKind, StringComparer.OrdinalIgnoreCase);
        hash.Add(Directives);
        hash.Add(CSharpParseOptions);

        return hash.CombinedHash;
    }

    public bool DesignTime
        => _flags.IsFlagSet(RazorParserOptionsFlags.DesignTime);

    /// <summary>
    ///  Gets a value which indicates whether the parser will parse only the leading directives. If <see langword="true"/>
    ///  the parser will halt at the first HTML content or C# code block. If <see langword="false"/> the whole document is parsed.
    /// </summary>
    /// <remarks>
    ///  Currently setting this option to <see langword="true"/> will result in only the first line of directives being parsed.
    ///  In a future release this may be updated to include all leading directive content.
    /// </remarks>
    public bool ParseLeadingDirectives
        => _flags.IsFlagSet(RazorParserOptionsFlags.ParseLeadingDirectives);

    public bool UseRoslynTokenizer
        => _flags.IsFlagSet(RazorParserOptionsFlags.UseRoslynTokenizer);

    internal bool EnableSpanEditHandlers
        => _flags.IsFlagSet(RazorParserOptionsFlags.EnableSpanEditHandlers);

    internal bool AllowMinimizedBooleanTagHelperAttributes
        => _flags.IsFlagSet(RazorParserOptionsFlags.AllowMinimizedBooleanTagHelperAttributes);

    internal bool AllowHtmlCommentsInTagHelpers
        => _flags.IsFlagSet(RazorParserOptionsFlags.AllowHtmlCommentsInTagHelpers);

    internal bool AllowComponentFileKind
        => _flags.IsFlagSet(RazorParserOptionsFlags.AllowComponentFileKind);

    internal bool AllowRazorInAllCodeBlocks
        => _flags.IsFlagSet(RazorParserOptionsFlags.AllowRazorInAllCodeBlocks);

    internal bool AllowUsingVariableDeclarations
        => _flags.IsFlagSet(RazorParserOptionsFlags.AllowUsingVariableDeclarations);

    internal bool AllowConditionalDataDashAttributes
        => _flags.IsFlagSet(RazorParserOptionsFlags.AllowConditionalDataDashAttributes);

    internal bool AllowCSharpInMarkupAttributeArea
        => _flags.IsFlagSet(RazorParserOptionsFlags.AllowCSharpInMarkupAttributeArea);

    internal bool AllowNullableForgivenessOperator
        => _flags.IsFlagSet(RazorParserOptionsFlags.AllowNullableForgivenessOperator);

    /// <summary>
    ///  Creates a new options instance with the specified Razor directives.
    /// </summary>
    internal RazorParserOptions WithDirectives(params ImmutableArray<DirectiveDescriptor> directives)
        => Directives.SequenceEqual(directives)
            ? this
            : new(LanguageVersion, FileKind, directives, CSharpParseOptions, _flags);


    /// <summary>
    ///  Creates a new options instance with the specified C# parse options.
    /// </summary>
    internal RazorParserOptions WithCSharpParseOptions(CSharpParseOptions csharpParseOptions)
        => CSharpParseOptions.Equals(csharpParseOptions)
            ? this
            : new(LanguageVersion, FileKind, Directives, csharpParseOptions, _flags);

    /// <summary>
    ///  Creates a new options instance with the specified flag values.
    /// </summary>
    internal RazorParserOptions WithFlags(
        Optional<bool> designTime = default,
        Optional<bool> parseLeadingDirectives = default,
        Optional<bool> useRoslynTokenizer = default,
        Optional<bool> enableSpanEditHandler = default,
        Optional<bool> allowMinimizedBooleanTagHelperAttributes = default,
        Optional<bool> allowHtmlCommentsInTagHelpers = default,
        Optional<bool> allowComponentFileKind = default,
        Optional<bool> allowRazorInAllCodeBlocks = default,
        Optional<bool> allowUsingVariableDeclarations = default,
        Optional<bool> allowConditionalDataDashAttributes = default,
        Optional<bool> allowCSharpInMarkupAttributeArea = default,
        Optional<bool> allowNullableForgivenessOperator = default)
    {
        var flags = _flags;

        if (designTime.HasValue)
        {
            flags.UpdateFlag(RazorParserOptionsFlags.DesignTime, designTime.Value);
        }

        if (parseLeadingDirectives.HasValue)
        {
            flags.UpdateFlag(RazorParserOptionsFlags.ParseLeadingDirectives, parseLeadingDirectives.Value);
        }

        if (useRoslynTokenizer.HasValue)
        {
            flags.UpdateFlag(RazorParserOptionsFlags.UseRoslynTokenizer, useRoslynTokenizer.Value);
        }

        if (enableSpanEditHandler.HasValue)
        {
            flags.UpdateFlag(RazorParserOptionsFlags.EnableSpanEditHandlers, enableSpanEditHandler.Value);
        }

        if (allowMinimizedBooleanTagHelperAttributes.HasValue)
        {
            flags.UpdateFlag(RazorParserOptionsFlags.AllowMinimizedBooleanTagHelperAttributes, allowMinimizedBooleanTagHelperAttributes.Value);
        }

        if (allowHtmlCommentsInTagHelpers.HasValue)
        {
            flags.UpdateFlag(RazorParserOptionsFlags.AllowHtmlCommentsInTagHelpers, allowHtmlCommentsInTagHelpers.Value);
        }

        if (allowComponentFileKind.HasValue)
        {
            flags.UpdateFlag(RazorParserOptionsFlags.AllowComponentFileKind, allowComponentFileKind.Value);
        }

        if (allowRazorInAllCodeBlocks.HasValue)
        {
            flags.UpdateFlag(RazorParserOptionsFlags.AllowRazorInAllCodeBlocks, allowRazorInAllCodeBlocks.Value);
        }

        if (allowUsingVariableDeclarations.HasValue)
        {
            flags.UpdateFlag(RazorParserOptionsFlags.AllowUsingVariableDeclarations, allowUsingVariableDeclarations.Value);
        }

        if (allowConditionalDataDashAttributes.HasValue)
        {
            flags.UpdateFlag(RazorParserOptionsFlags.AllowConditionalDataDashAttributes, allowConditionalDataDashAttributes.Value);
        }

        if (allowCSharpInMarkupAttributeArea.HasValue)
        {
            flags.UpdateFlag(RazorParserOptionsFlags.AllowCSharpInMarkupAttributeArea, allowCSharpInMarkupAttributeArea.Value);
        }

        if (allowNullableForgivenessOperator.HasValue)
        {
            flags.UpdateFlag(RazorParserOptionsFlags.AllowNullableForgivenessOperator, allowNullableForgivenessOperator.Value);
        }

        if (_flags == flags)
        {
            return this;
        }

        return new(LanguageVersion, FileKind, Directives, CSharpParseOptions, flags);
    }

    private static RazorParserOptionsFlags GetDefaultFlags(RazorLanguageVersion languageVersion, string fileKind)
    {
        RazorParserOptionsFlags flags = 0;

        flags.SetFlag(RazorParserOptionsFlags.AllowCSharpInMarkupAttributeArea);

        if (languageVersion >= RazorLanguageVersion.Version_2_1)
        {
            // Added in 2.1
            flags.SetFlag(RazorParserOptionsFlags.AllowMinimizedBooleanTagHelperAttributes);
            flags.SetFlag(RazorParserOptionsFlags.AllowHtmlCommentsInTagHelpers);
        }

        if (languageVersion >= RazorLanguageVersion.Version_3_0)
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

        if (languageVersion >= RazorLanguageVersion.Experimental)
        {
            flags.SetFlag(RazorParserOptionsFlags.AllowConditionalDataDashAttributes);
        }

        return flags;
    }
}
