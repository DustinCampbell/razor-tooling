// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Completion;

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
        => DocumentContext.TryGetTagHelperBinding(ContainingTagName, Attributes, ContainingParentTagName, ContainingParentIsTagHelper, out binding);

    public override ImmutableArray<TagHelperDescriptor> GetTagHelpersGivenTag(string prefixedName)
        => DocumentContext.GetTagHelpersGivenTag(prefixedName, ContainingTagName);

    public override ImmutableArray<TagHelperDescriptor> GetTagHelpersGivenParent()
        => DocumentContext.GetTagHelpersGivenParent(ContainingTagName);

    public override bool SatisfiesParentTag(TagMatchingRuleDescriptor rule)
        => TagHelperMatchingConventions.SatisfiesParentTag(rule, ContainingTagName.AsSpanOrDefault());
}
