// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

internal static class TagHelperKindExtensions
{
    public static bool IsAnyComponentDocument(this TagHelperKind kind)
    {
        return kind is >= TagHelperKind.Bind and <= TagHelperKind.RenderMode;
    }
}
