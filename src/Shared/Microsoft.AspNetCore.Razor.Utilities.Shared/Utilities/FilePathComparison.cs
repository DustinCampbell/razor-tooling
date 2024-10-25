// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.CodeAnalysis.Razor;

internal static class FilePathComparison
{
    private static StringComparison? _instance;

    public static StringComparison Instance
    {
        get
        {
            return _instance ??= PlatformInformation.IsLinux
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;
        }
    }
}
