// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language;

public static class CommonMetadata
{
    internal static readonly KeyValuePair<string, string?> BindAttributeGetSet
        = MakeTrue(ComponentMetadata.Bind.BindAttributeGetSet);
    internal static readonly KeyValuePair<string, string?> IsWeaklyTyped
        = MakeTrue(ComponentMetadata.Component.WeaklyTypedKey);

    internal static KeyValuePair<string, string?> MakeTrue(string key)
        => new(key, bool.TrueString);
    internal static KeyValuePair<string, string?> GloballyQualifiedTypeName(string value)
        => new(TagHelperMetadata.Common.GloballyQualifiedTypeName, value);
}
