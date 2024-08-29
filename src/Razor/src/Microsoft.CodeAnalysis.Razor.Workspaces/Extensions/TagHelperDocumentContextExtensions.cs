// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

internal static class TagHelperDocumentContextExtensions
{
    /// <summary>
    ///  Try to get all tag helpers that match the given HTML tag criteria.
    /// </summary>
    /// <param name="context">
    ///  The <see cref="TagHelperDocumentContext"/> to use.
    /// </param>
    /// <param name="tagName">
    ///  The name of the HTML tag to match. Providing a '*' tag name retrieves catch-all
    ///  <see cref="TagHelperDescriptor">TagHelperDescriptors</see>, that is, descriptors that
    ///  target any tag.
    /// </param>
    /// <param name="attributes">
    ///  The attributes on the HTML tag to match.
    /// </param>
    /// <param name="parentTagName">
    ///  The parent tag name of the specified tag.
    /// </param>
    /// <param name="parentIsTagHelper">
    ///  If the parent tag of the specified tag a tag helper.
    /// </param>
    /// <param name="binding">
    ///  Receives a <see cref="TagHelperBinding"/> representing the results of the match.
    /// </param>
    /// <returns>
    ///  Returns <see langword="true"/> if the match was successful; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryGetTagHelperBinding(
        this TagHelperDocumentContext context,
        string? tagName,
        ImmutableArray<KeyValuePair<string, string>> attributes,
        string? parentTagName,
        bool parentIsTagHelper,
        [NotNullWhen(true)] out TagHelperBinding? binding)
    {
        if (tagName is null)
        {
            binding = null;
            return false;
        }

        if (context.TagHelpers.Length == 0)
        {
            binding = null;
            return false;
        }

        var binder = context.GetBinder();

        binding = binder.GetBinding(tagName, attributes.NullToEmpty(), parentTagName, parentIsTagHelper);
        return binding is not null;
    }

    public static ImmutableArray<TagHelperDescriptor> GetTagHelpersGivenTag(this TagHelperDocumentContext context, string tagName, string? parentTag)
    {
        if (context.TagHelpers is not { Length: > 0 } tagHelpers)
        {
            return [];
        }

        var prefix = context.Prefix ?? string.Empty;
        if (!tagName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            // Can't possibly match TagHelpers, it doesn't start with the TagHelperPrefix.
            return [];
        }

        using var matchingDescriptors = new PooledArrayBuilder<TagHelperDescriptor>();

        var tagNameWithoutPrefix = tagName.AsSpan()[prefix.Length..];

        foreach (var tagHelper in tagHelpers)
        {
            foreach (var rule in tagHelper.TagMatchingRules)
            {
                if (TagHelperMatchingConventions.SatisfiesTagName(rule, tagNameWithoutPrefix) &&
                    TagHelperMatchingConventions.SatisfiesParentTag(rule, parentTag.AsSpanOrDefault()))
                {
                    matchingDescriptors.Add(tagHelper);
                    break;
                }
            }
        }

        return matchingDescriptors.DrainToImmutable();
    }

    public static ImmutableArray<TagHelperDescriptor> GetTagHelpersGivenParent(this TagHelperDocumentContext context, string? parentTag)
    {
        if (context.TagHelpers is not { Length: > 0 } tagHelpers)
        {
            return [];
        }

        using var matchingDescriptors = new PooledArrayBuilder<TagHelperDescriptor>();

        foreach (var descriptor in tagHelpers)
        {
            foreach (var rule in descriptor.TagMatchingRules)
            {
                if (TagHelperMatchingConventions.SatisfiesParentTag(rule, parentTag.AsSpanOrDefault()))
                {
                    matchingDescriptors.Add(descriptor);
                    break;
                }
            }
        }

        return matchingDescriptors.DrainToImmutable();
    }
}
