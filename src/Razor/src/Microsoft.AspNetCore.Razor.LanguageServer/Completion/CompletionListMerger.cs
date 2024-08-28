// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion;

internal static class CompletionListMerger
{
    private readonly record struct PropertyNames(string Name, string LowerInvariantName);

    private record class MergedCompletionListData(object Data1, object Data2)
    {
        public static readonly PropertyNames Data1Property = new (nameof(Data1), nameof(Data1).ToLowerInvariant());
        public static readonly PropertyNames Data2Property = new(nameof(Data2), nameof(Data2).ToLowerInvariant());
    }

    private static readonly object s_emptyData = new();

    public static VSInternalCompletionList? Merge(VSInternalCompletionList? razorCompletionList, VSInternalCompletionList? delegatedCompletionList)
    {
        if (razorCompletionList is null)
        {
            return delegatedCompletionList;
        }

        if (delegatedCompletionList?.Items is null)
        {
            return razorCompletionList;
        }

        EnsureMergeableCommitCharacters(razorCompletionList, delegatedCompletionList);
        EnsureMergeableData(razorCompletionList, delegatedCompletionList);

        var mergedIsIncomplete = razorCompletionList.IsIncomplete || delegatedCompletionList.IsIncomplete;
        CompletionItem[] mergedItems = [.. razorCompletionList.Items, .. delegatedCompletionList.Items];
        var mergedData = MergeData(razorCompletionList.Data, delegatedCompletionList.Data);
        var mergedCommitCharacters = razorCompletionList.CommitCharacters ?? delegatedCompletionList.CommitCharacters;
        var mergedSuggestionMode = razorCompletionList.SuggestionMode || delegatedCompletionList.SuggestionMode;

        // We don't fully support merging continue characters currently. Razor doesn't currently use them so delegated completion lists always win.
        var mergedContinueWithCharacters = razorCompletionList.ContinueCharacters ?? delegatedCompletionList.ContinueCharacters;

        // We don't fully support merging edit ranges currently. Razor doesn't currently use them so delegated completion lists always win.
        var mergedItemDefaultsEditRange = razorCompletionList.ItemDefaults?.EditRange ?? delegatedCompletionList.ItemDefaults?.EditRange;

        var mergedCompletionList = new VSInternalCompletionList()
        {
            CommitCharacters = mergedCommitCharacters,
            Data = mergedData,
            IsIncomplete = mergedIsIncomplete,
            Items = mergedItems,
            SuggestionMode = mergedSuggestionMode,
            ContinueCharacters = mergedContinueWithCharacters,
            ItemDefaults = new CompletionListItemDefaults()
            {
                EditRange = mergedItemDefaultsEditRange,
            }
        };

        return mergedCompletionList;
    }

    public static object? MergeData(object? data1, object? data2)
    {
        if (data1 is null)
        {
            return data2;
        }

        if (data2 is null)
        {
            return data1;
        }

        return new MergedCompletionListData(data1, data2);
    }

    public static bool TrySplit(object? data, out ImmutableArray<JsonElement> splitData)
    {
        if (data is null)
        {
            splitData = default;
            return false;
        }

        using var collector = new PooledArrayBuilder<JsonElement>();
        Split(data, ref collector.AsRef());

        if (collector.Count == 0)
        {
            splitData = default;
            return false;
        }

        splitData = collector.ToImmutable();
        return true;
    }

    private static void Split(object data, ref PooledArrayBuilder<JsonElement> collector)
    {
        if (data is MergedCompletionListData mergedData)
        {
            // Merged data adds an extra object wrapper around the original data, so remove
            // that to restore to the original form.
            Split(mergedData.Data1, ref collector);
            Split(mergedData.Data2, ref collector);
            return;
        }

        // We have to be agnostic to which serialization method the delegated servers use, including
        // the scenario where they use different ones, so we normalize the data to JObject.
        TrySplitJsonElement(data, ref collector);
        TrySplitJObject(data, ref collector);
    }

    private static void TrySplitJsonElement(object data, ref PooledArrayBuilder<JsonElement> collector)
    {
        if (data is not JsonElement jsonElement)
        {
            return;
        }

        if (ContainsProperty(jsonElement, MergedCompletionListData.Data1Property) &&
            ContainsProperty(jsonElement, MergedCompletionListData.Data2Property))
        {
            // Merged data
            var mergedCompletionListData = jsonElement.Deserialize<MergedCompletionListData>();

            if (mergedCompletionListData is null)
            {
                Debug.Fail("Merged completion list data is null, this should never happen.");
                return;
            }

            Split(mergedCompletionListData.Data1, ref collector);
            Split(mergedCompletionListData.Data2, ref collector);
        }
        else
        {
            collector.Add(jsonElement);
        }
    }

    private static void TrySplitJObject(object data, ref PooledArrayBuilder<JsonElement> collector)
    {
        if (data is not JObject jObject)
        {
            return;
        }

        if (ContainsProperty(jObject, MergedCompletionListData.Data1Property) &&
            ContainsProperty(jObject, MergedCompletionListData.Data2Property))
        {
            // Merged data
            var mergedCompletionListData = jObject.ToObject<MergedCompletionListData>();

            if (mergedCompletionListData is null)
            {
                Debug.Fail("Merged completion list data is null, this should never happen.");
                return;
            }

            Split(mergedCompletionListData.Data1, ref collector);
            Split(mergedCompletionListData.Data2, ref collector);
        }
        else
        {
            // Normal, non-merged data
            collector.Add(JsonDocument.Parse(jObject.ToString()).RootElement);
        }
    }

    private static bool ContainsProperty(JsonElement jsonElement, PropertyNames property)
        => jsonElement.TryGetProperty(property.Name, out _) || jsonElement.TryGetProperty(property.LowerInvariantName, out _);

    private static bool ContainsProperty(JObject jObject, PropertyNames property)
        => jObject.ContainsKey(property.Name) || jObject.ContainsKey(property.LowerInvariantName);

    private static void EnsureMergeableData(VSInternalCompletionList completionListA, VSInternalCompletionList completionListB)
    {
        if ((completionListA.Data == completionListB.Data || completionListA.Data is not null) && completionListB.Data is not null)
        {
            return;
        }

        // One of the completion lists have data while the other does not, we need to ensure that any non-data centric items don't get incorrect data associated

        // The candidate completion list will be one where we populate empty data for any null specifying data given we'll be merging
        // two completion lists together we don't want incorrect data to be inherited down
        var candidateCompletionList = completionListA.Data is null ? completionListA : completionListB;

        foreach (var item in candidateCompletionList.Items)
        {
            item.Data ??= s_emptyData;
        }
    }

    private static void EnsureMergeableCommitCharacters(VSInternalCompletionList completionListA, VSInternalCompletionList completionListB)
    {
        if (!InheritsCommitCharacters(completionListA) || !InheritsCommitCharacters(completionListB))
        {
            return;
        }

        // Need to merge commit characters because both are trying to inherit

        using var pooledListA = ListPool<VSInternalCompletionItem>.GetPooledObject(out var inheritableCompletionsA);
        using var pooledListB = ListPool<VSInternalCompletionItem>.GetPooledObject(out var inheritableCompletionsB);

        CollectCompletionsWithoutCommitCharacters(completionListA, inheritableCompletionsA);
        CollectCompletionsWithoutCommitCharacters(completionListB, inheritableCompletionsB);

        // Decide which completion list has more items that benefit from "inheriting" commit characters.
        var (completionListToStopInheriting, completionItemsToStopInheriting) = inheritableCompletionsA.Count >= inheritableCompletionsB.Count
            ? (completionListB, inheritableCompletionsB)
            : (completionListA, inheritableCompletionsA);

        var commitCharacters = completionListToStopInheriting.CommitCharacters is not null
            ? completionListToStopInheriting.CommitCharacters
            : completionListToStopInheriting.ItemDefaults?.CommitCharacters;

        for (var i = 0; i < completionItemsToStopInheriting.Count; i++)
        {
            completionItemsToStopInheriting[i].VsCommitCharacters = commitCharacters;
        }

        completionListToStopInheriting.CommitCharacters = null;

        if (completionListToStopInheriting.ItemDefaults is { } itemDefaults)
        {
            itemDefaults.CommitCharacters = null;
        }

        static bool InheritsCommitCharacters(VSInternalCompletionList completionList)
        {
            return completionList is { CommitCharacters: not null } or { ItemDefaults.CommitCharacters: not null };
        }

        static void CollectCompletionsWithoutCommitCharacters(VSInternalCompletionList completionList, List<VSInternalCompletionItem> collector)
        {
            foreach (var item in completionList.Items)
            {
                if (item is VSInternalCompletionItem { CommitCharacters: null, VsCommitCharacters: null } vsItem)
                {
                    collector.Add(vsItem);
                }
            }
        }
    }
}
