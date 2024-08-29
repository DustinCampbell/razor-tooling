// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal sealed class AttributeCompletionContext
{
    public TagHelperDocumentContext DocumentContext { get; }
    public IEnumerable<string> ExistingCompletions { get; }
    public string CurrentTagName { get; }
    public string? CurrentAttributeName { get; }
    public ImmutableArray<KeyValuePair<string, string>> Attributes { get; }
    public string? CurrentParentTagName { get; }
    public bool CurrentParentIsTagHelper { get; }
    public Func<string, bool> InHTMLSchema { get; }

    public AttributeCompletionContext(
        TagHelperDocumentContext documentContext,
        IEnumerable<string> existingCompletions,
        string currentTagName,
        string? currentAttributeName,
        ImmutableArray<KeyValuePair<string, string>> attributes,
        string? currentParentTagName,
        bool currentParentIsTagHelper,
        Func<string, bool>? inHTMLSchema = null)
    {
        DocumentContext = documentContext;
        ExistingCompletions = existingCompletions;
        CurrentTagName = currentTagName;
        CurrentAttributeName = currentAttributeName;
        Attributes = attributes.NullToEmpty();
        CurrentParentTagName = currentParentTagName;
        CurrentParentIsTagHelper = currentParentIsTagHelper;
        InHTMLSchema = inHTMLSchema ?? HtmlFacts.IsHtmlTagName;
    }
}
