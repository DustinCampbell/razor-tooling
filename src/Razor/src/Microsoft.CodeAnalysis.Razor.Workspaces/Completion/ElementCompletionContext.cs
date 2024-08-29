// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal class ElementCompletionContext
{
    public TagHelperDocumentContext DocumentContext { get; }
    public IEnumerable<string> ExistingCompletions { get; }
    public string? ContainingTagName { get; }
    public ImmutableArray<KeyValuePair<string, string>> Attributes { get; }
    public string? ContainingParentTagName { get; }
    public bool ContainingParentIsTagHelper { get; }
    public Func<string, bool> InHTMLSchema { get; }

    public ElementCompletionContext(
        TagHelperDocumentContext documentContext,
        IEnumerable<string>? existingCompletions,
        string? containingTagName,
        ImmutableArray<KeyValuePair<string, string>> attributes,
        string? containingParentTagName,
        bool containingParentIsTagHelper,
        Func<string, bool> inHTMLSchema)
    {
        DocumentContext = documentContext ?? throw new ArgumentNullException(nameof(documentContext));
        ExistingCompletions = existingCompletions ?? Array.Empty<string>();
        ContainingTagName = containingTagName;
        Attributes = attributes;
        ContainingParentTagName = containingParentTagName;
        ContainingParentIsTagHelper = containingParentIsTagHelper;
        InHTMLSchema = inHTMLSchema ?? throw new ArgumentNullException(nameof(inHTMLSchema));
    }

    // Note: The properties and methods below capture nuances between the Razor LSP editor's tag helper completion
    // and the Razor legacy editor's tag helper completion. These are called by the TagHelperCompletionService to
    // compute results for the appropriate editor.
    //
    // The defaults below represent the LSP editor's behavior.
    // LegacyElementCompletionContext overrides these members to tweak tag helper completion for the legacy editor.

    public virtual bool ShouldCheckAttributeRules => true;

    public virtual bool InitializeWithExistingCompletions => false;

    public virtual bool TryGetTagHelperBinding([NotNullWhen(true)] out TagHelperBinding? binding)
    {
        binding = TagHelperFacts.GetTagHelperBinding(
            DocumentContext,
            ContainingParentTagName,
            Attributes,
            parentTag: null,
            parentIsTagHelper: false);

        return binding is not null;
    }

    public virtual ImmutableArray<TagHelperDescriptor> GetTagHelpersGivenTag(string prefixedName)
        => TagHelperFacts.GetTagHelpersGivenTag(DocumentContext, prefixedName, ContainingParentTagName);

    public virtual ImmutableArray<TagHelperDescriptor> GetTagHelpersGivenParent()
        => TagHelperFacts.GetTagHelpersGivenParent(DocumentContext, ContainingParentTagName);

    public virtual bool SatisfiesParentTag(TagMatchingRuleDescriptor rule)
        => TagHelperMatchingConventions.SatisfiesParentTag(rule, ContainingParentTagName.AsSpanOrDefault());
}
