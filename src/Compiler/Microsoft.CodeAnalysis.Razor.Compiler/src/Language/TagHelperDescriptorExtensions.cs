// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

namespace Microsoft.AspNetCore.Razor.Language;

public static class TagHelperDescriptorExtensions
{
    public static bool IsDefaultKind(this TagHelperDescriptor tagHelper)
    {
        ArgHelper.ThrowIfNull(tagHelper);

        return tagHelper.Kind == TagHelperKind.Default;
    }

    public static bool KindUsesDefaultTagHelperRuntime(this TagHelperDescriptor tagHelper)
    {
        ArgHelper.ThrowIfNull(tagHelper);

        return tagHelper.RuntimeKind == RuntimeKind.Default;
    }
}
