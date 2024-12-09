// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.ProjectSystem;

internal sealed class ProjectWorkspaceState : IEquatable<ProjectWorkspaceState>
{
    public static readonly ProjectWorkspaceState Default = new(tagHelpers: [], LanguageVersion.Default);

    public ImmutableArray<TagHelperDescriptor> TagHelpers { get; }
    public LanguageVersion CSharpLanguageVersion { get; }

    private ProjectWorkspaceState(
        ImmutableArray<TagHelperDescriptor> tagHelpers,
        LanguageVersion csharpLanguageVersion)
    {
        TagHelpers = tagHelpers.NullToEmpty();
        CSharpLanguageVersion = csharpLanguageVersion;
    }

    public static ProjectWorkspaceState Create(
        ImmutableArray<TagHelperDescriptor> tagHelpers,
        LanguageVersion csharpLanguageVersion = LanguageVersion.Default)
        => tagHelpers.IsDefaultOrEmpty && csharpLanguageVersion == LanguageVersion.Default
            ? Default
            : new(tagHelpers, csharpLanguageVersion);

    public static ProjectWorkspaceState Create(LanguageVersion csharpLanguageVersion)
        => csharpLanguageVersion == LanguageVersion.Default
            ? Default
            : new(tagHelpers: [], csharpLanguageVersion);

    public override bool Equals(object? obj)
        => obj is ProjectWorkspaceState other && Equals(other);

    public bool Equals(ProjectWorkspaceState? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null ||
            CSharpLanguageVersion != other.CSharpLanguageVersion)
        {
            return false;
        }

        // PERF: Dig into the underlying arrays of TagHelpers to perform more efficient checks.
        var span = TagHelpers.AsSpan();
        var otherSpan = other.TagHelpers.AsSpan();

        // Returns true if the spans point to the same memory and their lengths are the same.
        if (span == otherSpan)
        {
            return true;
        }

        if (span.Length != otherSpan.Length)
        {
            return false;
        }

        while (span is [var item, ..] && otherSpan is [var otherItem, ..])
        {
            if (!ReferenceEquals(item, otherItem) && !item.Equals(otherItem))
            {
                return false;
            }

            span = span[1..];
            otherSpan = otherSpan[1..];
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = HashCodeCombiner.Start();

        hash.Add(TagHelpers);
        hash.Add(CSharpLanguageVersion);

        return hash.CombinedHash;
    }
}
