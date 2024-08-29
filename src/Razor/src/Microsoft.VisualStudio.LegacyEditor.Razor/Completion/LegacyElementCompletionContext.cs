// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.VisualStudio.LegacyEditor.Razor.Completion;

// This class is utilized entirely by the legacy Razor editor and should not be touched except when specifically working on the legacy editor to avoid breaking functionality.

internal sealed class LegacyElementCompletionContext : ElementCompletionContext
{
    public LegacyElementCompletionContext(
        TagHelperDocumentContext documentContext,
        IEnumerable<string>? existingCompletions,
        string? containingTagName,
        ImmutableArray<KeyValuePair<string, string>> attributes,
        string? containingParentTagName,
        bool containingParentIsTagHelper,
        Func<string, bool> inHTMLSchema)
        : base(documentContext, existingCompletions, containingTagName, attributes, containingParentTagName, containingParentIsTagHelper, inHTMLSchema)
    {
    }

    public override bool ShouldCheckAttributeRules => false;

    public override bool InitializeWithExistingCompletions => true;

    public override bool TryGetTagHelperBinding([NotNullWhen(true)] out TagHelperBinding? binding)
    {
        binding = TagHelperFacts.GetTagHelperBinding(
            DocumentContext,
            ContainingTagName,
            Attributes,
            ContainingParentTagName,
            ContainingParentIsTagHelper);

        return binding is not null;
    }

    public override ImmutableArray<TagHelperDescriptor> GetTagHelpersGivenTag(string prefixedName)
        => TagHelperFacts.GetTagHelpersGivenTag(DocumentContext, prefixedName, ContainingTagName);

    public override ImmutableArray<TagHelperDescriptor> GetTagHelpersGivenParent()
        => TagHelperFacts.GetTagHelpersGivenParent(DocumentContext, ContainingTagName);

    public override bool SatisfiesParentTag(TagMatchingRuleDescriptor rule)
        => TagHelperMatchingConventions.SatisfiesParentTag(rule, ContainingTagName.AsSpanOrDefault());
}
