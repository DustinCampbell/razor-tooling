// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

internal static class TagHelperDescriptorExtensions
{
    public static TagHelperDescriptor WithName(this TagHelperDescriptor tagHelper, string name)
        => new(
            tagHelper.Kind, tagHelper.RuntimeKind, name, tagHelper.AssemblyName,
            tagHelper.TypeName, tagHelper.TypeNamespace, tagHelper.TypeNameIdentifier,
            tagHelper.DisplayName, tagHelper.Flags, tagHelper.DocumentationObject,
            tagHelper.TagOutputHint, tagHelper.TagMatchingRules, tagHelper.BoundAttributes,
            tagHelper.AllowedChildTags, tagHelper.Metadata, tagHelper.Diagnostics);
}
