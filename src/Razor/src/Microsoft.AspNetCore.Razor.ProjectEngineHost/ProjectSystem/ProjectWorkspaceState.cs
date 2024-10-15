// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.ProjectSystem;

internal readonly record struct ProjectWorkspaceState(
    ImmutableArray<TagHelperDescriptor> TagHelpers,
    LanguageVersion CSharpLanguageVersion = default)
{
    public static readonly ProjectWorkspaceState Default = new(TagHelpers: [], CSharpLanguageVersion: LanguageVersion.Default);

    private readonly ImmutableArray<TagHelperDescriptor> _tagHelpers = TagHelpers.NullToEmpty();

    public ImmutableArray<TagHelperDescriptor> TagHelpers
    {
        // We call NullToEmpty() here to ensure that default(ProjectWorkspaceState).TagHelpers returns [].
        get => _tagHelpers.NullToEmpty();
        init => _tagHelpers = value;
    }

    public LanguageVersion CSharpLanguageVersion { get; init; } = CSharpLanguageVersion;

    public bool IsDefault
        => CSharpLanguageVersion == LanguageVersion.Default &&
           _tagHelpers.IsDefaultOrEmpty;

    public bool Equals(ProjectWorkspaceState other)
        => CSharpLanguageVersion == other.CSharpLanguageVersion &&
           TagHelpers.SequenceEqual(other.TagHelpers);

    public override int GetHashCode()
    {
        var hash = HashCodeCombiner.Start();

        hash.Add(TagHelpers);
        hash.Add(CSharpLanguageVersion);

        return hash.CombinedHash;
    }
}
