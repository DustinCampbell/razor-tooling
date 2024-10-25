// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Utilities;

internal record struct FilePath
{
    private static StringComparer? s_comparer;
    private static StringComparison? s_comparison;

    public static StringComparer Comparer
        => s_comparer ??= PlatformInformation.IsLinux ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

    public static StringComparison Comparison
        => s_comparison ??= PlatformInformation.IsLinux ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    public string Value { get; }

    private string? _normalizedValue;

    public string NormalizedValue
        => _normalizedValue ??= PathNormalization.Normalize(Value);

    private FilePath(string value)
    {
        Value = value;
    }

    public readonly bool Equals(FilePath other)
        => Comparer.Equals(Value, other.Value);

    public override readonly int GetHashCode()
        => Comparer.GetHashCode(Value);

    public static implicit operator FilePath(string value)
        => new(value);

    public static implicit operator string(FilePath path)
        => path.Value;
}
