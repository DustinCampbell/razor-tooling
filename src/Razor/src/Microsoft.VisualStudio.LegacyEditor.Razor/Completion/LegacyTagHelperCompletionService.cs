// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.VisualStudio.LegacyEditor.Razor.Completion;

// This class is utilized entirely by the legacy Razor editor and should not be touched except when specifically working on the legacy editor to avoid breaking functionality.

[Export(typeof(ITagHelperCompletionService))]
internal sealed class LegacyTagHelperCompletionService : AbstractTagHelperCompletionService
{
    protected override bool UseExistingCompletions => true;
    protected override bool CheckAttributeRules => false;

    protected override bool TryGetTagHelperBinding(ElementCompletionContext context, [NotNullWhen(true)] out TagHelperBinding? binding)
    {
        binding = TagHelperFacts.GetTagHelperBinding(
            context.DocumentContext,
            context.ContainingTagName,
            context.Attributes,
            context.ContainingParentTagName,
            context.ContainingParentIsTagHelper);

        return binding is not null;
    }

    protected override ImmutableArray<TagHelperDescriptor> GetTagHelpersGivenTag(ElementCompletionContext context, string prefixedName)
        => TagHelperFacts.GetTagHelpersGivenTag(context.DocumentContext, prefixedName, context.ContainingTagName);

    protected override ImmutableArray<TagHelperDescriptor> GetTagHelpersGivenParent(ElementCompletionContext context)
        => TagHelperFacts.GetTagHelpersGivenParent(context.DocumentContext, context.ContainingTagName);

    protected override bool SatisfiesParentTag(TagMatchingRuleDescriptor rule, ElementCompletionContext context)
        => TagHelperMatchingConventions.SatisfiesParentTag(rule, context.ContainingTagName.AsSpanOrDefault());
}
