// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion;

internal class LspTagHelperCompletionService : AbstractTagHelperCompletionService
{
    protected override bool UseExistingCompletions => false;
    protected override bool CheckAttributeRules => true;

    protected override bool TryGetTagHelperBinding(ElementCompletionContext context, [NotNullWhen(true)] out TagHelperBinding? binding)
    {
        binding = TagHelperFacts.GetTagHelperBinding(
            context.DocumentContext,
            context.ContainingParentTagName,
            context.Attributes,
            parentTag: null,
            parentIsTagHelper: false);

        return binding is not null;
    }

    protected override ImmutableArray<TagHelperDescriptor> GetTagHelpersGivenTag(ElementCompletionContext context, string prefixedName)
        => TagHelperFacts.GetTagHelpersGivenTag(context.DocumentContext, prefixedName, context.ContainingParentTagName);

    protected override ImmutableArray<TagHelperDescriptor> GetTagHelpersGivenParent(ElementCompletionContext context)
        => TagHelperFacts.GetTagHelpersGivenParent(context.DocumentContext, context.ContainingParentTagName);

    protected override bool SatisfiesParentTag(TagMatchingRuleDescriptor rule, ElementCompletionContext context)
        => TagHelperMatchingConventions.SatisfiesParentTag(rule, context.ContainingParentTagName.AsSpanOrDefault());
}
