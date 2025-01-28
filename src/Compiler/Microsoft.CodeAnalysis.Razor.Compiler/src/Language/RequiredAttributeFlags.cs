// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using static Microsoft.AspNetCore.Razor.Language.RequiredAttributeDescriptor;

namespace Microsoft.AspNetCore.Razor.Language;

[Flags]
internal enum RequiredAttributeFlags : byte
{
    CaseSensitive = 1 << 0,
    IsDirectiveAttribute = 1 << 1,

    NameComparisonMask = 0b00000100,

    ValueComparison_FullMatch = 0b00001000,
    ValueComparison_PrefixMatch = 0b00010000,
    ValueComparison_SuffixMatch = 0b00011000,

    Default = CaseSensitive,
    ValueComparisonMask = ValueComparison_SuffixMatch
}

internal static class RequiredAttributeFlagsExtensions
{
    private const int NameComparisonShift = 2;
    private const RequiredAttributeFlags NameComparisonMask = (RequiredAttributeFlags)(1 << NameComparisonShift);

    private const int ValueComparisonShift = 3;
    private const RequiredAttributeFlags ValueComparisonMask = (RequiredAttributeFlags)(0b11 << ValueComparisonShift);

    public static NameComparisonMode GetNameComparison(this RequiredAttributeFlags flags)
        => (NameComparisonMode)((byte)(flags & NameComparisonMask) >> NameComparisonShift);

    public static ValueComparisonMode GetValueComparison(this RequiredAttributeFlags flags)
        => (ValueComparisonMode)((byte)(flags & ValueComparisonMask) >> ValueComparisonShift);

    public static void SetNameComparison(this ref RequiredAttributeFlags flags, NameComparisonMode value)
    {
        flags &= ~NameComparisonMask; // clear bits
        flags |= (RequiredAttributeFlags)((byte)value << NameComparisonShift);
    }

    public static void SetValueComparison(this ref RequiredAttributeFlags flags, ValueComparisonMode value)
    {
        flags &= ~ValueComparisonMask;
        flags |= (RequiredAttributeFlags)((byte)value << ValueComparisonShift);
    }
}
