// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Utilities;

internal struct FilePath
{
    private static StringComparer? s_comparer;
    private static StringComparison? s_comparison;

    public static StringComparer Comparer
        => s_comparer ??= PlatformInformation.IsLinux ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

    public static StringComparison Comparison
        => s_comparison ??= PlatformInformation.IsLinux ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    public string Value { get; }
}
