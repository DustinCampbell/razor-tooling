// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal sealed class AttributeCompletionResult
{
    public ImmutableDictionary<string, ImmutableArray<BoundAttributeDescriptor>> Completions { get; }

    private AttributeCompletionResult(ImmutableDictionary<string, ImmutableArray<BoundAttributeDescriptor>> completions)
    {
        Completions = completions;
    }

    internal static AttributeCompletionResult Create(ImmutableDictionary<string, ImmutableArray<BoundAttributeDescriptor>> completions)
    {
        return new AttributeCompletionResult(completions);
    }
}
