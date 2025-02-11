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

    private readonly Flags _flags;

    public RazorLanguageVersion LanguageVersion { get; }
    public string FileKind { get; }
    public ImmutableArray<DirectiveDescriptor> Directives { get; }
    public CSharpParseOptions CSharpParseOptions { get; }

    private RazorParserOptions(
        RazorLanguageVersion languageVersion,
        string fileKind,
        ImmutableArray<DirectiveDescriptor> directives,
        CSharpParseOptions csharpParseOptions,
        Flags flags)
    {
        if (flags.IsFlagSet(Flags.ParseLeadingDirectives) &&
            flags.IsFlagSet(Flags.UseRoslynTokenizer))
        {
            ThrowHelper.ThrowInvalidOperationException($"{nameof(Flags.ParseLeadingDirectives)} and {nameof(Flags.UseRoslynTokenizer)} can't both be true.");
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
        => _flags.IsFlagSet(Flags.DesignTime);

    /// <summary>
    ///  Gets a value which indicates whether the parser will parse only the leading directives. If <see langword="true"/>
    ///  the parser will halt at the first HTML content or C# code block. If <see langword="false"/> the whole document is parsed.
    /// </summary>
    /// <remarks>
    ///  Currently setting this option to <see langword="true"/> will result in only the first line of directives being parsed.
    ///  In a future release this may be updated to include all leading directive content.
    /// </remarks>
    public bool ParseLeadingDirectives
        => _flags.IsFlagSet(Flags.ParseLeadingDirectives);

    public bool UseRoslynTokenizer
        => _flags.IsFlagSet(Flags.UseRoslynTokenizer);

    internal bool EnableSpanEditHandlers
        => _flags.IsFlagSet(Flags.EnableSpanEditHandlers);

    internal bool AllowMinimizedBooleanTagHelperAttributes
        => _flags.IsFlagSet(Flags.AllowMinimizedBooleanTagHelperAttributes);

    internal bool AllowHtmlCommentsInTagHelpers
        => _flags.IsFlagSet(Flags.AllowHtmlCommentsInTagHelpers);

    internal bool AllowComponentFileKind
        => _flags.IsFlagSet(Flags.AllowComponentFileKind);

    internal bool AllowRazorInAllCodeBlocks
        => _flags.IsFlagSet(Flags.AllowRazorInAllCodeBlocks);

    internal bool AllowUsingVariableDeclarations
        => _flags.IsFlagSet(Flags.AllowUsingVariableDeclarations);

    internal bool AllowConditionalDataDashAttributes
        => _flags.IsFlagSet(Flags.AllowConditionalDataDashAttributes);

    internal bool AllowCSharpInMarkupAttributeArea
        => _flags.IsFlagSet(Flags.AllowCSharpInMarkupAttributeArea);

    internal bool AllowNullableForgivenessOperator
        => _flags.IsFlagSet(Flags.AllowNullableForgivenessOperator);

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
            flags.UpdateFlag(Flags.DesignTime, designTime.Value);
        }

        if (parseLeadingDirectives.HasValue)
        {
            flags.UpdateFlag(Flags.ParseLeadingDirectives, parseLeadingDirectives.Value);
        }

        if (useRoslynTokenizer.HasValue)
        {
            flags.UpdateFlag(Flags.UseRoslynTokenizer, useRoslynTokenizer.Value);
        }

        if (enableSpanEditHandler.HasValue)
        {
            flags.UpdateFlag(Flags.EnableSpanEditHandlers, enableSpanEditHandler.Value);
        }

        if (allowMinimizedBooleanTagHelperAttributes.HasValue)
        {
            flags.UpdateFlag(Flags.AllowMinimizedBooleanTagHelperAttributes, allowMinimizedBooleanTagHelperAttributes.Value);
        }

        if (allowHtmlCommentsInTagHelpers.HasValue)
        {
            flags.UpdateFlag(Flags.AllowHtmlCommentsInTagHelpers, allowHtmlCommentsInTagHelpers.Value);
        }

        if (allowComponentFileKind.HasValue)
        {
            flags.UpdateFlag(Flags.AllowComponentFileKind, allowComponentFileKind.Value);
        }

        if (allowRazorInAllCodeBlocks.HasValue)
        {
            flags.UpdateFlag(Flags.AllowRazorInAllCodeBlocks, allowRazorInAllCodeBlocks.Value);
        }

        if (allowUsingVariableDeclarations.HasValue)
        {
            flags.UpdateFlag(Flags.AllowUsingVariableDeclarations, allowUsingVariableDeclarations.Value);
        }

        if (allowConditionalDataDashAttributes.HasValue)
        {
            flags.UpdateFlag(Flags.AllowConditionalDataDashAttributes, allowConditionalDataDashAttributes.Value);
        }

        if (allowCSharpInMarkupAttributeArea.HasValue)
        {
            flags.UpdateFlag(Flags.AllowCSharpInMarkupAttributeArea, allowCSharpInMarkupAttributeArea.Value);
        }

        if (allowNullableForgivenessOperator.HasValue)
        {
            flags.UpdateFlag(Flags.AllowNullableForgivenessOperator, allowNullableForgivenessOperator.Value);
        }

        if (_flags == flags)
        {
            return this;
        }

        return new(LanguageVersion, FileKind, Directives, CSharpParseOptions, flags);
    }
}
