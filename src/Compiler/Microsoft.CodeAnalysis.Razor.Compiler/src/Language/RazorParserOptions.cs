// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorParserOptions
{
    private readonly RazorParserOptionsFlags _flags;

    public ImmutableArray<DirectiveDescriptor> Directives { get; }
    internal string FileKind { get; }
    public CSharpParseOptions CSharpParseOptions { get; }
    public RazorLanguageVersion Version { get; } = RazorLanguageVersion.Latest;

    internal RazorParserFeatureFlags FeatureFlags { get; /* Testing Only */ init; }

    public bool DesignTime
        => _flags.IsFlagSet(RazorParserOptionsFlags.DesignTime);

    /// <summary>
    /// Gets a value which indicates whether the parser will parse only the leading directives. If <c>true</c>
    /// the parser will halt at the first HTML content or C# code block. If <c>false</c> the whole document is parsed.
    /// </summary>
    /// <remarks>
    /// Currently setting this option to <c>true</c> will result in only the first line of directives being parsed.
    /// In a future release this may be updated to include all leading directive content.
    /// </remarks>
    public bool ParseLeadingDirectives
        => _flags.IsFlagSet(RazorParserOptionsFlags.ParseLeadingDirectives);

    public bool UseRoslynTokenizer
        => _flags.IsFlagSet(RazorParserOptionsFlags.UseRoslynTokenizer);

    internal bool EnableSpanEditHandlers
        => _flags.IsFlagSet(RazorParserOptionsFlags.EnableSpanEditHandlers);

    internal RazorParserOptions(
        RazorParserOptionsFlags flags,
        ImmutableArray<DirectiveDescriptor> directives,
        RazorLanguageVersion version,
        string fileKind,
        CSharpParseOptions csharpParseOptions)
    {
        _flags = flags;

        if (flags.IsFlagSet(RazorParserOptionsFlags.ParseLeadingDirectives) &&
            flags.IsFlagSet(RazorParserOptionsFlags.UseRoslynTokenizer))
        {
            ThrowHelper.ThrowInvalidOperationException($"Cannot set {nameof(RazorParserOptionsFlags.ParseLeadingDirectives)} and {nameof(RazorParserOptionsFlags.UseRoslynTokenizer)} to true simultaneously.");
        }

        fileKind ??= FileKinds.Legacy;

        Directives = directives.NullToEmpty();
        Version = version;
        FeatureFlags = RazorParserFeatureFlags.Create(Version, fileKind);
        FileKind = fileKind;
        CSharpParseOptions = csharpParseOptions;
    }

    public static RazorParserOptions CreateDefault()
    {
        return new RazorParserOptions(
            flags: RazorParserOptionsFlags.DefaultFlags,
            directives: [],
            version: RazorLanguageVersion.Latest,
            fileKind: FileKinds.Legacy,
            csharpParseOptions: CSharpParseOptions.Default);
    }

    public static RazorParserOptions Create(Action<RazorParserOptionsBuilder> configure)
    {
        return Create(configure, fileKind: FileKinds.Legacy);
    }

    public static RazorParserOptions Create(Action<RazorParserOptionsBuilder> configure, string fileKind)
    {
        ArgHelper.ThrowIfNull(configure);

        var builder = new RazorParserOptionsBuilder(fileKind, version: RazorLanguageVersion.Latest, designTime: false);
        configure(builder);
        var options = builder.Build();

        return options;
    }

    public static RazorParserOptions CreateDesignTime(Action<RazorParserOptionsBuilder> configure)
    {
        return CreateDesignTime(configure, fileKind: FileKinds.Legacy);
    }

    public static RazorParserOptions CreateDesignTime(Action<RazorParserOptionsBuilder> configure, string fileKind)
    {
        ArgHelper.ThrowIfNull(configure);

        var builder = new RazorParserOptionsBuilder(fileKind, version: RazorLanguageVersion.Latest, designTime: true);
        configure(builder);
        var options = builder.Build();

        return options;
    }
}
