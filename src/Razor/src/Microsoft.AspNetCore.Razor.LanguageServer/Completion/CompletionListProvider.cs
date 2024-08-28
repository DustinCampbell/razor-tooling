// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Completion.Delegation;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion;

internal class CompletionListProvider
{
    private readonly RazorCompletionListProvider _razorCompletionListProvider;
    private readonly DelegatedCompletionListProvider _delegatedCompletionListProvider;

    private readonly string[] _allTriggerCharacters;

    public CompletionListProvider(RazorCompletionListProvider razorCompletionListProvider, DelegatedCompletionListProvider delegatedCompletionListProvider)
    {
        _razorCompletionListProvider = razorCompletionListProvider;
        _delegatedCompletionListProvider = delegatedCompletionListProvider;

        using var _ = StringHashSetPool.Ordinal.GetPooledObject(out var set);

        set.AddRange(razorCompletionListProvider.TriggerCharacters.Items);
        set.AddRange(delegatedCompletionListProvider.TriggerCharacters.Items);

        _allTriggerCharacters = [.. set];
    }

    public string[] AllTriggerCharacters => [.. _allTriggerCharacters];

    public async Task<VSInternalCompletionList?> GetCompletionListAsync(
        int absoluteIndex,
        VSInternalCompletionContext completionContext,
        DocumentContext documentContext,
        VSInternalClientCapabilities clientCapabilities,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        VSInternalCompletionList? delegatedCompletionList = null;
        VSInternalCompletionList? razorCompletionList = null;

        // First we delegate to get completion items from the individual language server
        if (completionContext.IsValidTrigger(_delegatedCompletionListProvider.TriggerCharacters))
        {
            delegatedCompletionList = await _delegatedCompletionListProvider
                .GetCompletionListAsync(absoluteIndex, completionContext, documentContext, clientCapabilities, correlationId, cancellationToken)
                .ConfigureAwait(false);
        }

        if (completionContext.IsValidTrigger(_razorCompletionListProvider.TriggerCharacters))
        {
            // Extract the items we got back from the delegated server, to inform tag helper completion.
            HashSet<string>? existingItems = null;

            if (delegatedCompletionList?.Items is { } items)
            {
                existingItems = new(capacity: items.Length);

                foreach (var item in items)
                {
                    existingItems.Add(item.Label);
                }
            }

            // Now we get the Razor completion list, using information from the actual language server if necessary
            razorCompletionList = await _razorCompletionListProvider
                .GetCompletionListAsync(absoluteIndex, completionContext, documentContext, clientCapabilities, existingItems, cancellationToken)
                .ConfigureAwait(false);
        }

        return CompletionListMerger.Merge(razorCompletionList, delegatedCompletionList);
    }
}
