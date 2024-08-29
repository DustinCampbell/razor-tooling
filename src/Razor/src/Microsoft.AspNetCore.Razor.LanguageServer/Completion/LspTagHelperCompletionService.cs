// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion;

internal class LspTagHelperCompletionService : AbstractTagHelperCompletionService
{
    private static readonly HashSet<TagHelperDescriptor> s_emptyHashSet = new();

    public override ElementCompletionResult GetElementCompletions(ElementCompletionContext completionContext)
    {
        if (TryAddAllowedChildrenCompletions(completionContext, out var elementCompletions))
        {
            // If the containing element is already a TagHelper and only allows certain children.
            return ElementCompletionResult.Create(elementCompletions);
        }

        elementCompletions = [];

        var tagAttributes = completionContext.Attributes;

        var catchAllDescriptors = new HashSet<TagHelperDescriptor>();
        var prefix = completionContext.DocumentContext.Prefix ?? string.Empty;

        using var collector = new PooledArrayBuilder<TagHelperDescriptor>();
        TagHelperFacts.CollectTagHelpersGivenParent(completionContext.DocumentContext, completionContext.ContainingParentTagName, ref collector.AsRef());
        var possibleChildDescriptors = FilterFullyQualifiedCompletions(in collector);

        foreach (var possibleDescriptor in possibleChildDescriptors)
        {
            var addRuleCompletions = false;
            var checkAttributeRules = true;
            var outputHint = possibleDescriptor.TagOutputHint;

            foreach (var rule in possibleDescriptor.TagMatchingRules)
            {
                if (!TagHelperMatchingConventions.SatisfiesParentTag(rule, completionContext.ContainingParentTagName.AsSpanOrDefault()))
                {
                    continue;
                }

                if (rule.TagName == TagHelperMatchingConventions.ElementCatchAllName)
                {
                    catchAllDescriptors.Add(possibleDescriptor);
                }
                else if (elementCompletions.ContainsKey(rule.TagName))
                {
                    // If we've previously added a completion item for this rules tag, then we want to add this item
                    addRuleCompletions = true;
                }
                else if (completionContext.ContainsExistingCompletion(rule.TagName))
                {
                    // If Html wants to show a completion item for rules tag, then we want to add this item
                    addRuleCompletions = true;
                }
                else if (outputHint != null)
                {
                    // If the current descriptor has an output hint we need to make sure it shows up only when its output hint would normally show up.
                    // Example: We have a MyTableTagHelper that has an output hint of "table" and a MyTrTagHelper that has an output hint of "tr".
                    // If we try typing in a situation like this: <body > | </body>
                    // We'd expect to only get "my-table" as a completion because the "body" tag doesn't allow "tr" tags.
                    addRuleCompletions = completionContext.ContainsExistingCompletion(outputHint);
                }
                else if (!completionContext.InHTMLSchema(rule.TagName) || rule.TagName.Any(char.IsUpper))
                {
                    // If there is an unknown HTML schema tag that doesn't exist in the current completion we should add it. This happens for
                    // TagHelpers that target non-schema oriented tags.
                    // The second condition is a workaround for the fact that InHTMLSchema does a case insensitive comparison.
                    // We want completions to not dedupe by casing. E.g, we want to show both <div> and <DIV> completion items separately.
                    addRuleCompletions = true;

                    // If the tag is not in the Html schema, then don't check attribute rules. Normally we want to check them so that
                    // users don't see html tag and tag helper completions for the same thing, where the tag helper doesn't apply. In
                    // cases where the tag is not html, we want to show it even if it doesn't apply, as the user could fix that later.
                    checkAttributeRules = false;
                }

                // If we think this completion should be added based on tag name, that's great, but lets also make sure the attributes are correct
                if (addRuleCompletions && (!checkAttributeRules || TagHelperMatchingConventions.SatisfiesAttributes(rule, tagAttributes)))
                {
                    UpdateCompletions(prefix + rule.TagName, possibleDescriptor, elementCompletions);
                }
            }
        }

        // We needed to track all catch-alls and update their completions after all other completions have been completed.
        // This way, any TagHelper added completions will also have catch-alls listed under their entries.
        foreach (var catchAllDescriptor in catchAllDescriptors)
        {
            foreach (var (completionTagName, tagHelperDescriptors) in elementCompletions)
            {
                if (tagHelperDescriptors.Count > 0 ||
                    (!prefix.IsNullOrEmpty() && completionTagName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    // The current completion either has other TagHelper's associated with it or is prefixed with a non-empty
                    // TagHelper prefix.
                    UpdateCompletions(completionTagName, catchAllDescriptor, elementCompletions, tagHelperDescriptors);
                }
            }
        }

        return ElementCompletionResult.Create(elementCompletions);

        static void UpdateCompletions(string tagName, TagHelperDescriptor possibleDescriptor, Dictionary<string, HashSet<TagHelperDescriptor>> elementCompletions, HashSet<TagHelperDescriptor>? tagHelperDescriptors = null)
        {
            if (possibleDescriptor.BoundAttributes.Any(static boundAttribute => boundAttribute.IsDirectiveAttribute))
            {
                // This is a TagHelper that ultimately represents a DirectiveAttribute. In classic Razor TagHelper land TagHelpers with bound attribute descriptors
                // are valuable to show in the completion list to understand what was possible for a certain tag; however, with Blazor directive attributes stand
                // on their own and shouldn't be indicated at the element level completion.
                return;
            }

            HashSet<TagHelperDescriptor>? existingRuleDescriptors;
            if (tagHelperDescriptors is not null)
            {
                existingRuleDescriptors = tagHelperDescriptors;
            }
            else if (!elementCompletions.TryGetValue(tagName, out existingRuleDescriptors))
            {
                existingRuleDescriptors = new HashSet<TagHelperDescriptor>();
                elementCompletions[tagName] = existingRuleDescriptors;
            }

            existingRuleDescriptors.Add(possibleDescriptor);
        }
    }

    private static bool TryAddAllowedChildrenCompletions(
        ElementCompletionContext completionContext,
        [NotNullWhen(true)] out Dictionary<string, HashSet<TagHelperDescriptor>>? elementCompletions)
    {
        elementCompletions = null;

        if (completionContext.ContainingTagName is null)
        {
            // If we're at the root then there's no containing TagHelper to specify allowed children.
            return false;
        }

        var prefix = completionContext.DocumentContext.Prefix ?? string.Empty;

        var binding = TagHelperFacts.GetTagHelperBinding(
            completionContext.DocumentContext,
            completionContext.ContainingParentTagName,
            completionContext.Attributes,
            parentTag: null,
            parentIsTagHelper: false);

        if (binding is null)
        {
            // Containing tag is not a TagHelper; therefore, it allows any children.
            return false;
        }

        foreach (var descriptor in binding.Descriptors)
        {
            foreach (var childTag in descriptor.AllowedChildTags)
            {
                var prefixedName = string.Concat(prefix, childTag.Name);
                var descriptors = TagHelperFacts.GetTagHelpersGivenTag(
                    completionContext.DocumentContext,
                    prefixedName,
                    completionContext.ContainingParentTagName);

                if (descriptors.Length == 0)
                {
                    elementCompletions ??= [];

                    if (!elementCompletions.ContainsKey(prefixedName))
                    {
                        elementCompletions[prefixedName] = s_emptyHashSet;
                    }

                    continue;
                }

                elementCompletions ??= [];
                if (!elementCompletions.TryGetValue(prefixedName, out var existingRuleDescriptors))
                {
                    existingRuleDescriptors = new HashSet<TagHelperDescriptor>();
                    elementCompletions[prefixedName] = existingRuleDescriptors;
                }

                existingRuleDescriptors.AddRange(descriptors);
            }
        }

        return elementCompletions?.Count > 0;
    }

    private static ImmutableArray<TagHelperDescriptor> FilterFullyQualifiedCompletions(ref readonly PooledArrayBuilder<TagHelperDescriptor> possibleChildDescriptors)
    {
        // Iterate once through the list to tease apart fully qualified and short name TagHelpers
        using var fullyQualifiedTagHelpers = new PooledArrayBuilder<TagHelperDescriptor>();
        var shortNameTagHelpers = new HashSet<TagHelperDescriptor>(ShortNameToFullyQualifiedComparer.Instance);

        foreach (var descriptor in possibleChildDescriptors)
        {
            if (descriptor.IsComponentFullyQualifiedNameMatch)
            {
                fullyQualifiedTagHelpers.Add(descriptor);
            }
            else
            {
                shortNameTagHelpers.Add(descriptor);
            }
        }

        // Re-combine the short named & fully qualified TagHelpers but filter out any fully qualified TagHelpers that have a short
        // named representation already.
        using var filteredList = new PooledArrayBuilder<TagHelperDescriptor>(capacity: shortNameTagHelpers.Count);
        filteredList.AddRange(shortNameTagHelpers);

        foreach (var fullyQualifiedTagHelper in fullyQualifiedTagHelpers)
        {
            if (!shortNameTagHelpers.Contains(fullyQualifiedTagHelper))
            {
                // Unimported completion item that isn't represented in a short named form.
                filteredList.Add(fullyQualifiedTagHelper);
            }
            else
            {
                // There's already a shortname variant of this item, don't include it.
            }
        }

        return filteredList.DrainToImmutable();
    }
}
