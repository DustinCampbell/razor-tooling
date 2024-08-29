// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal class TagHelperCompletionService : ITagHelperCompletionService
{
    private static readonly HashSet<TagHelperDescriptor> s_emptyHashSet = new();

    private static readonly ObjectPool<HashSet<TagHelperDescriptor>> s_shortNameSetPool
        = HashSetPool<TagHelperDescriptor>.Create(ShortNameToFullyQualifiedComparer.Instance);

    // This API attempts to understand a users context as they're typing in a Razor file to provide TagHelper based attribute IntelliSense.
    //
    // Scenarios for TagHelper attribute IntelliSense follows:
    // 1. TagHelperDescriptor's have matching required attribute names
    //  -> Provide IntelliSense for the required attributes of those descriptors to lead users towards a TagHelperified element.
    // 2. TagHelperDescriptor entirely applies to current element. Tag name, attributes, everything is fulfilled.
    //  -> Provide IntelliSense for the bound attributes for the applied descriptors.
    //
    // Within each of the above scenarios if an attribute completion has a corresponding bound attribute we associate it with the corresponding
    // BoundAttributeDescriptor. By doing this a user can see what C# type a TagHelper expects for the attribute.
    public AttributeCompletionResult GetAttributeCompletions(AttributeCompletionContext completionContext)
    {
        if (completionContext is null)
        {
            throw new ArgumentNullException(nameof(completionContext));
        }

        var attributeCompletions = completionContext.ExistingCompletions.ToDictionary(
            completion => completion,
            _ => new HashSet<BoundAttributeDescriptor>(),
            StringComparer.OrdinalIgnoreCase);

        var documentContext = completionContext.DocumentContext;
        var descriptorsForTag = documentContext.GetTagHelpersGivenTag(completionContext.CurrentTagName, completionContext.CurrentParentTagName);
        if (descriptorsForTag.Length == 0)
        {
            // If the current tag has no possible descriptors then we can't have any additional attributes.
            return AttributeCompletionResult.Create(attributeCompletions);
        }

        var prefix = documentContext.Prefix ?? string.Empty;
        Debug.Assert(completionContext.CurrentTagName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        using var _ = HashSetPool<TagHelperDescriptor>.GetPooledObject(out var applicableDescriptors);

        if (documentContext.TryGetTagHelperBinding(
            completionContext.CurrentTagName,
            completionContext.Attributes,
            completionContext.CurrentParentTagName,
            completionContext.CurrentParentIsTagHelper,
            out var applicableTagHelperBinding))
        {
            applicableDescriptors.AddRange(applicableTagHelperBinding.Descriptors);
        }

        var unprefixedTagName = completionContext.CurrentTagName[prefix.Length..];

        if (!completionContext.InHTMLSchema(unprefixedTagName) &&
            applicableDescriptors.All(descriptor => descriptor.TagOutputHint is null))
        {
            // This isn't a known HTML tag and no descriptor has an output element hint. Remove all previous completions.
            attributeCompletions.Clear();
        }

        foreach (var descriptor in descriptorsForTag)
        {
            if (applicableDescriptors.Contains(descriptor))
            {
                foreach (var attributeDescriptor in descriptor.BoundAttributes)
                {
                    if (!attributeDescriptor.Name.IsNullOrEmpty())
                    {
                        UpdateCompletions(attributeDescriptor.Name, attributeDescriptor);
                    }

                    if (!string.IsNullOrEmpty(attributeDescriptor.IndexerNamePrefix))
                    {
                        UpdateCompletions(attributeDescriptor.IndexerNamePrefix + "...", attributeDescriptor);
                    }
                }
            }
            else
            {
                var htmlNameToBoundAttribute = new Dictionary<string, BoundAttributeDescriptor>(StringComparer.OrdinalIgnoreCase);
                foreach (var attributeDescriptor in descriptor.BoundAttributes)
                {
                    if (attributeDescriptor.Name != null)
                    {
                        htmlNameToBoundAttribute[attributeDescriptor.Name] = attributeDescriptor;
                    }

                    if (!attributeDescriptor.IndexerNamePrefix.IsNullOrEmpty())
                    {
                        htmlNameToBoundAttribute[attributeDescriptor.IndexerNamePrefix] = attributeDescriptor;
                    }
                }

                foreach (var rule in descriptor.TagMatchingRules)
                {
                    foreach (var requiredAttribute in rule.Attributes)
                    {
                        if (htmlNameToBoundAttribute.TryGetValue(requiredAttribute.Name, out var attributeDescriptor))
                        {
                            UpdateCompletions(requiredAttribute.DisplayName, attributeDescriptor);
                        }
                        else
                        {
                            UpdateCompletions(requiredAttribute.DisplayName, possibleDescriptor: null);
                        }
                    }
                }
            }
        }

        var completionResult = AttributeCompletionResult.Create(attributeCompletions);
        return completionResult;

        void UpdateCompletions(string attributeName, BoundAttributeDescriptor? possibleDescriptor)
        {
            if (completionContext.Attributes.Any(attribute => string.Equals(attribute.Key, attributeName, StringComparison.OrdinalIgnoreCase)) &&
                (completionContext.CurrentAttributeName is null ||
                !string.Equals(attributeName, completionContext.CurrentAttributeName, StringComparison.OrdinalIgnoreCase)))
            {
                // Attribute is already present on this element and it is not the attribute in focus.
                // It shouldn't exist in the completion list.
                return;
            }

            if (!attributeCompletions.TryGetValue(attributeName, out var rules))
            {
                rules = new HashSet<BoundAttributeDescriptor>();
                attributeCompletions[attributeName] = rules;
            }

            if (possibleDescriptor != null)
            {
                rules.Add(possibleDescriptor);
            }
        }
    }

    public ElementCompletionResult GetElementCompletions(ElementCompletionContext completionContext)
    {
        var elementCompletions = new Dictionary<string, HashSet<TagHelperDescriptor>>(StringComparer.Ordinal);

        AddAllowedChildrenCompletions(completionContext, elementCompletions);

        if (elementCompletions.Count > 0)
        {
            // If the containing element is already a TagHelper and only allows certain children.
            return ElementCompletionResult.Create(elementCompletions);
        }

        if (completionContext.InitializeWithExistingCompletions)
        {
            foreach (var completion in completionContext.ExistingCompletions)
            {
                elementCompletions.Add(completion, []);
            }
        }

        // Acquire a pooled HashSet to collect catch-all tag helpers, i.e. those that match any tag name.
        using var pooledSet = HashSetPool<TagHelperDescriptor>.GetPooledObject();
        var catchAllTagHelpers = pooledSet.Object;

        var prefix = completionContext.DocumentContext.Prefix ?? string.Empty;
        var tagHelpers = completionContext.GetTagHelpersGivenParent();

        using var deduplicatedTagHelpers = new PooledArrayBuilder<TagHelperDescriptor>(tagHelpers.Length);
        DeduplicateTagHelpers(tagHelpers, ref deduplicatedTagHelpers.AsRef());

        foreach (var tagHelper in deduplicatedTagHelpers)
        {
            var addRuleCompletions = false;
            var checkAttributeRules = completionContext.ShouldCheckAttributeRules;
            var outputHint = tagHelper.TagOutputHint;

            foreach (var rule in tagHelper.TagMatchingRules)
            {
                if (!completionContext.SatisfiesParentTag(rule))
                {
                    continue;
                }

                if (rule.TagName == TagHelperMatchingConventions.ElementCatchAllName)
                {
                    catchAllTagHelpers.Add(tagHelper);
                }
                else if (elementCompletions.ContainsKey(rule.TagName))
                {
                    // If we've previously added a completion item for this rules tag, then we want to add this item
                    addRuleCompletions = true;
                }
                else if (completionContext.ExistingCompletions.Contains(rule.TagName))
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
                    addRuleCompletions = completionContext.ExistingCompletions.Contains(outputHint);
                }
                else if (!completionContext.InHTMLSchema(rule.TagName) || rule.TagName.Any(char.IsUpper))
                {
                    // If there is an unknown HTML schema tag that doesn't exist in the current completion we should add it. This happens for
                    // TagHelpers that target non-schema oriented tags.
                    // The second condition is a workaround for the fact that InHTMLSchema does a case insensitive comparison.
                    // We want completions to not dedupe by casing. E.g, we want to show both <div> and <DIV> completion items separately.
                    addRuleCompletions = true;

                    if (completionContext.ShouldCheckAttributeRules)
                    {
                        // If the tag is not in the Html schema, then don't check attribute rules. Normally we want to check them so that
                        // users don't see html tag and tag helper completions for the same thing, where the tag helper doesn't apply. In
                        // cases where the tag is not html, we want to show it even if it doesn't apply, as the user could fix that later.
                        checkAttributeRules = false;
                    }
                }

                // If we think this completion should be added based on tag name, that's great, but lets also make sure the attributes are correct
                if (addRuleCompletions && (!checkAttributeRules || TagHelperMatchingConventions.SatisfiesAttributes(rule, completionContext.Attributes)))
                {
                    UpdateCompletions(prefix + rule.TagName, tagHelper, elementCompletions);
                }
            }
        }

        // We needed to track all catch-alls and update their completions after all other completions have been completed.
        // This way, any TagHelper added completions will also have catch-alls listed under their entries.
        foreach (var tagHelper in catchAllTagHelpers)
        {
            foreach (var (elementTagName, elementTagHelpers) in elementCompletions)
            {
                if (elementTagHelpers.Count > 0 ||
                    (!string.IsNullOrEmpty(prefix) && elementTagName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    // The current completion either has other TagHelper's associated with it or is prefixed with a non-empty
                    // TagHelper prefix.
                    UpdateCompletions(elementTagName, tagHelper, elementCompletions, elementTagHelpers);
                }
            }
        }

        var result = ElementCompletionResult.Create(elementCompletions);
        return result;

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

    private static void AddAllowedChildrenCompletions(ElementCompletionContext completionContext, Dictionary<string, HashSet<TagHelperDescriptor>> elementCompletions)
    {
        if (completionContext.ContainingTagName is null)
        {
            // If we're at the root then there's no containing TagHelper to specify allowed children.
            return;
        }

        if (!completionContext.TryGetTagHelperBinding(out var binding))
        {
            // Containing tag is not a TagHelper; therefore, it allows any children.
            return;
        }

        var prefix = completionContext.DocumentContext.Prefix ?? string.Empty;

        foreach (var descriptor in binding.Descriptors)
        {
            foreach (var childTag in descriptor.AllowedChildTags)
            {
                var prefixedName = string.Concat(prefix, childTag.Name);
                var descriptors = completionContext.GetTagHelpersGivenTag(prefixedName);

                if (descriptors.Length == 0)
                {
                    elementCompletions.TryAdd(prefixedName, s_emptyHashSet);
                    continue;
                }

                var existingDescriptors = elementCompletions.GetOrCreate(prefixedName);
                existingDescriptors.AddRange(descriptors);
            }
        }
    }

    /// <summary>
    ///  Deduplicate the specified set of tag helpers to those that share the same short-name.
    /// </summary>
    private static void DeduplicateTagHelpers(ImmutableArray<TagHelperDescriptor> tagHelpers, ref PooledArrayBuilder<TagHelperDescriptor> collector)
    {
        // Acquire a pooled HashSet to keep track of short-named tag helpers and de-dupe them.
        using var pooledSet = s_shortNameSetPool.GetPooledObject();
        var shortNameTagHelperSet = pooledSet.Object;

        using var fullyQualifiedTagHelpers = new PooledArrayBuilder<TagHelperDescriptor>(tagHelpers.Length);

        // Iterate through the tag helpers to determine which ones are short-named vs. fully-qualified.
        foreach (var tagHelper in tagHelpers)
        {
            if (tagHelper.IsComponentFullyQualifiedNameMatch)
            {
                // This is a fully-qualified tag helper, so we need to remember it for later.
                fullyQualifiedTagHelpers.Add(tagHelper);
            }
            else if (shortNameTagHelperSet.Add(tagHelper))
            {
                // We haven't seen this short-named tag helper yet, so go ahead and collect it.
                collector.Add(tagHelper);
            }
        }

        // Now, iterate through the fully-qualified tag helpers again and collect any that we didn't already
        // see in a short-named form.

        foreach (var tagHelper in fullyQualifiedTagHelpers)
        {
            if (!shortNameTagHelperSet.Contains(tagHelper))
            {
                // Collect this fully-qualified tag helper that we have seen a version of yet.
                collector.Add(tagHelper);
            }
        }
    }
}
