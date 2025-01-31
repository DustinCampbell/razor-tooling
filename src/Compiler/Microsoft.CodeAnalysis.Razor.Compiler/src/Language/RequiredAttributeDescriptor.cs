// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RequiredAttributeDescriptor : TagHelperObject<RequiredAttributeDescriptor>
{
    private readonly RequiredAttributeFlags _flags;
    private TagMatchingRuleDescriptor? _parent;

    public string Name { get; }
    public string? Value { get; }
    public string DisplayName { get; }

    internal RequiredAttributeFlags Flags => _flags;
    public bool CaseSensitive => (_flags & RequiredAttributeFlags.CaseSensitive) != 0;
    public bool IsDirectiveAttribute => (_flags & RequiredAttributeFlags.IsDirectiveAttribute) != 0;

    public NameComparisonMode NameComparison => _flags.GetNameComparison();
    public ValueComparisonMode ValueComparison => _flags.GetValueComparison();

    public MetadataCollection Metadata { get; }

    public TagMatchingRuleDescriptor Parent => _parent.AssumeNotNull();

    internal RequiredAttributeDescriptor(
        string name,
        string? value,
        RequiredAttributeFlags flags,
        string displayName,
        ImmutableArray<RazorDiagnostic> diagnostics,
        MetadataCollection metadata)
        : base(diagnostics)
    {
        Name = name;
        Value = value;
        _flags = flags;
        DisplayName = displayName;
        Metadata = metadata ?? MetadataCollection.Empty;
    }

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.AppendData(Name);
        builder.AppendData(Value);
        builder.AppendData((int)Flags);
        builder.AppendData(DisplayName);
        builder.AppendData(CaseSensitive);
        builder.AppendData(Metadata.Checksum);
    }

    internal void SetParent(TagMatchingRuleDescriptor parent)
    {
        Debug.Assert(_parent is null);
        Debug.Assert(parent.Attributes.Contains(this));

        _parent = parent;
    }

    public override string ToString()
    {
        return DisplayName ?? base.ToString()!;
    }

    /// <summary>
    /// Acceptable <see cref="Name"/> comparison modes.
    /// </summary>
    public enum NameComparisonMode : byte
    {
        /// <summary>
        /// HTML attribute name case insensitively matches <see cref="Name"/>.
        /// </summary>
        FullMatch,

        /// <summary>
        /// HTML attribute name case insensitively starts with <see cref="Name"/>.
        /// </summary>
        PrefixMatch,
    }

    /// <summary>
    /// Acceptable <see cref="Value"/> comparison modes.
    /// </summary>
    public enum ValueComparisonMode : byte
    {
        /// <summary>
        /// HTML attribute value always matches <see cref="Value"/>.
        /// </summary>
        None,

        /// <summary>
        /// HTML attribute value case sensitively matches <see cref="Value"/>.
        /// </summary>
        FullMatch,

        /// <summary>
        /// HTML attribute value case sensitively starts with <see cref="Value"/>.
        /// </summary>
        PrefixMatch,

        /// <summary>
        /// HTML attribute value case sensitively ends with <see cref="Value"/>.
        /// </summary>
        SuffixMatch,
    }
}
