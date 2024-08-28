// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion;

internal static class VSInternalCompletionItemExtensions
{
    private const string ResultIdKey = "_resultId";

    private static readonly Dictionary<RazorCommitCharacter, VSInternalCommitCharacter> s_vsInternalCommitCharacterCache = [];

    /// <summary>
    ///  Retrieves a cached <see cref="VSInternalCommitCharacter"/> for the given <see cref="RazorCommitCharacter"/>
    ///  or creates a new one and adds it to the cache.
    /// </summary>
    private static VSInternalCommitCharacter GetOrCreateVsInternalCommitCharacter(RazorCommitCharacter commitCharacter)
    {
        lock (s_vsInternalCommitCharacterCache)
        {
            var result = s_vsInternalCommitCharacterCache.GetOrAdd(
                commitCharacter,
                static ch => new() { Character = ch.Character, Insert = ch.Insert });

            Debug.Assert(
                commitCharacter.Character == result.Character && commitCharacter.Insert == result.Insert,
                "Cached VSInternalCommitCharacter has been modified from the original!");

            return result;
        }
    }

    public static bool TryGetCompletionListResultIds(this VSInternalCompletionItem completion, out ImmutableArray<int> resultIds)
    {
        if (!CompletionListMerger.TrySplit(completion.Data, out var splitData))
        {
            resultIds = default;
            return false;
        }

        using var ids = new PooledArrayBuilder<int>();
        for (var i = 0; i < splitData.Length; i++)
        {
            var data = splitData[i];
            if (data.TryGetProperty(ResultIdKey, out var resultIdElement) &&
                resultIdElement.TryGetInt32(out var resultId))
            {
                ids.Add(resultId);
            }
        }

        if (ids.Count > 0)
        {
            resultIds = ids.DrainToImmutable();
            return true;
        }

        resultIds = default;
        return false;
    }

    /// <summary>
    ///  Sets the correct "commit characters" property on the given LSP completion item.
    /// </summary>
    /// <param name="completionItem">The LSP completion item to update.</param>
    /// <param name="razorCompletionItem">The Razor completion item whose commit characters will be used.</param>
    /// <param name="clientCapabilities">The available client capabilities.</param>
    /// <remarks>
    ///  If <see cref="VSInternalClientCapabilities.SupportsVisualStudioExtensions"/> is <see langword="true"/>,
    ///  the <see cref="VSInternalCompletionItem.VsCommitCharacters"/> property will be set; if <see langword="false"/>,
    ///  <see cref="CompletionItem.CommitCharacters"/> will be set.
    /// </remarks>
    public static void SetupCommitCharacters(
        this VSInternalCompletionItem completionItem,
        RazorCompletionItem razorCompletionItem,
        VSInternalClientCapabilities clientCapabilities)
    {
        var commitCharacters = razorCompletionItem.CommitCharacters;
        if (commitCharacters.IsEmpty)
        {
            return;
        }

        // In the calculations below, SelectAsArray returns a new ImmutableArray<T>, so we can safely use its internal array.

        var supportsVSExtensions = clientCapabilities.SupportsVisualStudioExtensions;
        if (supportsVSExtensions)
        {
            completionItem.VsCommitCharacters = commitCharacters
                .SelectAsArray(GetOrCreateVsInternalCommitCharacter)
                .Unsafe().AsArray();
        }
        else
        {
            completionItem.CommitCharacters = commitCharacters
                .SelectAsArray(static ch => ch.Character)
                .Unsafe().AsArray();
        }
    }
}
