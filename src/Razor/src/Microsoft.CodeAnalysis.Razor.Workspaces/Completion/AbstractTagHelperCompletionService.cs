// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.Extensions.ObjectPool;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal abstract partial class AbstractTagHelperCompletionService : ITagHelperCompletionService
{
    private static readonly ObjectPool<CompletionBuilder<BoundAttributeDescriptor>> s_attributeCompletionBuilderPool
        = DefaultPool.Create(Policy<BoundAttributeDescriptor>.Instance);
    private static readonly ObjectPool<CompletionBuilder<TagHelperDescriptor>> s_elementCompletionBuilderPool
        = DefaultPool.Create(Policy<TagHelperDescriptor>.Instance);

    protected static PooledObject<CompletionBuilder<BoundAttributeDescriptor>> GetPooledAttributeCompletionBuilder(out CompletionBuilder<BoundAttributeDescriptor> builder)
        => s_attributeCompletionBuilderPool.GetPooledObject(out builder);

    protected static PooledObject<CompletionBuilder<TagHelperDescriptor>> GetPooledElementCompletionBuilder(out CompletionBuilder<TagHelperDescriptor> builder)
        => s_elementCompletionBuilderPool.GetPooledObject(out builder);

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
        using var pooledCompletionBuilder = GetPooledAttributeCompletionBuilder(out var completionBuilder);

        foreach (var completion in completionContext.ExistingCompletions)
        {
            completionBuilder.Add(completion);
        }

        var documentContext = completionContext.DocumentContext;
        var descriptorsForTag = TagHelperFacts.GetTagHelpersGivenTag(
            documentContext,
            completionContext.CurrentTagName,
            completionContext.CurrentParentTagName);

        if (descriptorsForTag.Length == 0)
        {
            // If the current tag has no possible descriptors then we can't have any additional attributes.
            return AttributeCompletionResult.Create(completionBuilder.ToImmutable());
        }

        var prefix = documentContext.Prefix ?? string.Empty;
        Debug.Assert(completionContext.CurrentTagName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        var applicableTagHelperBinding = TagHelperFacts.GetTagHelperBinding(
            documentContext,
            completionContext.CurrentTagName,
            completionContext.Attributes,
            completionContext.CurrentParentTagName,
            completionContext.CurrentParentIsTagHelper);

        using var pooledHashSet = HashSetPool<TagHelperDescriptor>.GetPooledObject(out var applicableDescriptors);

        if (applicableTagHelperBinding is { Descriptors: var descriptors })
        {
            applicableDescriptors.AddRange(descriptors);
        }

        var unprefixedTagName = completionContext.CurrentTagName[prefix.Length..];

        if (!completionContext.InHTMLSchema(unprefixedTagName) &&
            applicableDescriptors.All(static d => d.TagOutputHint is null))
        {
            // This isn't a known HTML tag and no descriptor has an output element hint. Remove all previous completions.
            completionBuilder.Clear();
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

                    if (!attributeDescriptor.IndexerNamePrefix.IsNullOrEmpty())
                    {
                        UpdateCompletions(attributeDescriptor.IndexerNamePrefix + "...", attributeDescriptor);
                    }
                }
            }
            else
            {
                using var pooledDictionary = StringDictionaryPool<BoundAttributeDescriptor>.OrdinalIgnoreCase.GetPooledObject(out var htmlNameToBoundAttribute);

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

        return AttributeCompletionResult.Create(completionBuilder.ToImmutable());

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

            if (possibleDescriptor is null)
            {
                completionBuilder.Add(attributeName);
            }
            else
            {
                completionBuilder.Add(attributeName, possibleDescriptor);
            }
        }
    }

    public abstract ElementCompletionResult GetElementCompletions(ElementCompletionContext completionContext);
}
