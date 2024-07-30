// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed record class RazorParserOptions
{
    public static RazorParserOptions Default { get; } = new(
        directives: [],
        designTime: false,
        parseLeadingDirectives: false,
        version: RazorLanguageVersion.Latest,
        fileKind: FileKinds.Legacy,
        enableSpanEditHandlers: false);

    public ImmutableArray<DirectiveDescriptor> Directives { get; }
    public bool DesignTime { get; }

    /// <summary>
    /// Gets a value which indicates whether the parser will parse only the leading directives. If <c>true</c>
    /// the parser will halt at the first HTML content or C# code block. If <c>false</c> the whole document is parsed.
    /// </summary>
    /// <remarks>
    /// Currently setting this option to <c>true</c> will result in only the first line of directives being parsed.
    /// In a future release this may be updated to include all leading directive content.
    /// </remarks>
    public bool ParseLeadingDirectives { get; }

    public RazorLanguageVersion Version { get; }

    internal string FileKind { get; }

    internal bool EnableSpanEditHandlers { get; }

    internal RazorParserFeatureFlags FeatureFlags { get; /* Testing Only */ init; }

    internal RazorParserOptions(
        ImmutableArray<DirectiveDescriptor> directives,
        bool designTime,
        bool parseLeadingDirectives,
        RazorLanguageVersion version,
        string fileKind,
        bool enableSpanEditHandlers)
    {
        Directives = directives.NullToEmpty();
        DesignTime = designTime;
        ParseLeadingDirectives = parseLeadingDirectives;
        Version = version;
        FeatureFlags = RazorParserFeatureFlags.Create(Version, fileKind);
        FileKind = fileKind;
        EnableSpanEditHandlers = enableSpanEditHandlers;
    }

    public bool Equals(RazorParserOptions? other)
        => other is not null &&
           DesignTime == other.DesignTime &&
           ParseLeadingDirectives == other.ParseLeadingDirectives &&
           EnableSpanEditHandlers == other.EnableSpanEditHandlers &&
           StringComparer.OrdinalIgnoreCase.Equals(FileKind, other.FileKind) &&
           Version == other.Version &&
           Directives.SequenceEqual(other.Directives);

    public override int GetHashCode()
    {
        var hash = HashCodeCombiner.Start();

        hash.Add(DesignTime);
        hash.Add(ParseLeadingDirectives);
        hash.Add(EnableSpanEditHandlers);
        hash.Add(FileKind, StringComparer.OrdinalIgnoreCase);
        hash.Add(Version);
        hash.Add(Directives);

        return hash.CombinedHash;
    }

    public static RazorParserOptions Create(Action<RazorParserOptionsBuilder> configure)
        => Create(configure, fileKind: FileKinds.Legacy);

    public static RazorParserOptions Create(Action<RazorParserOptionsBuilder> configure, string? fileKind)
    {
        ArgHelper.ThrowIfNull(configure);

        var builder = new RazorParserOptionsBuilder(designTime: false, version: RazorLanguageVersion.Latest, fileKind ?? FileKinds.Legacy);
        configure(builder);
        var options = builder.Build();

        return options;
    }

    public static RazorParserOptions CreateDesignTime(Action<RazorParserOptionsBuilder> configure)
        => CreateDesignTime(configure, fileKind: null);

    public static RazorParserOptions CreateDesignTime(Action<RazorParserOptionsBuilder> configure, string? fileKind)
    {
        ArgHelper.ThrowIfNull(configure);

        var builder = new RazorParserOptionsBuilder(designTime: true, version: RazorLanguageVersion.Latest, fileKind ?? FileKinds.Legacy);
        configure(builder);
        var options = builder.Build();

        return options;
    }
}
