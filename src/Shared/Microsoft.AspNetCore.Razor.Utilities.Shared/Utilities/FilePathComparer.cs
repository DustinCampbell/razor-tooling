// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.CodeAnalysis.Razor;

internal static class FilePathComparer
{
    private static StringComparer? _instance;

    public static StringComparer Instance
    {
        get
        {
            return _instance ??= PlatformInformation.IsLinux
                ? StringComparer.Ordinal
                : StringComparer.OrdinalIgnoreCase;
        }
    }
}
