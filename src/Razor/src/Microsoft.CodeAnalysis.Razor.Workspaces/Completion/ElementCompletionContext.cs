// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal sealed class ElementCompletionContext
{
    private readonly HashSet<string>? _existingCompletionSet;

    public TagHelperDocumentContext DocumentContext { get; }
    public IEnumerable<string> ExistingCompletions => _existingCompletionSet ?? [];
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
        ContainingTagName = containingTagName;
        Attributes = attributes;
        ContainingParentTagName = containingParentTagName;
        ContainingParentIsTagHelper = containingParentIsTagHelper;
        InHTMLSchema = inHTMLSchema ?? throw new ArgumentNullException(nameof(inHTMLSchema));

        switch (existingCompletions)
        {
            case HashSet<string> set:
                // We were handed a HashSet<string>, so we'll use it directly.
                _existingCompletionSet = set;
                break;

            case IEnumerable<string> enumerable:
                // We were handed an IEnumerable<string>, so we'll create a HashSet<T> from it.
                _existingCompletionSet = new HashSet<string>(enumerable);
                break;
        }
    }

    public bool ContainsExistingCompletion(string text)
        => _existingCompletionSet?.Contains(text) ?? false;
}
