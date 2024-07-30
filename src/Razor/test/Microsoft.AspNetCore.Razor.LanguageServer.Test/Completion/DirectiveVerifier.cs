// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion;

internal static class DirectiveVerifier
{
    public static readonly Action<CompletionItem>[] DefaultDirectiveCollectionVerifiers;

    static DirectiveVerifier()
    {
        using var builder = new PooledArrayBuilder<Action<CompletionItem>>(capacity: DirectiveCompletionItemProvider.MvcDefaultDirectives.Length * 2);

        foreach (var directive in DirectiveCompletionItemProvider.MvcDefaultDirectives)
        {
            builder.Add(item => Assert.Equal(directive.Directive, item.InsertText));
            builder.Add(item => AssertDirectiveSnippet(item, directive.Directive));
        }

        DefaultDirectiveCollectionVerifiers = builder.ToArray();
    }

    private static void AssertDirectiveSnippet(CompletionItem completionItem, string directive)
    {
        Assert.StartsWith(directive, completionItem.InsertText);
        Assert.Equal(DirectiveCompletionItemProvider.SingleLineDirectiveSnippets[directive].InsertText, completionItem.InsertText);
        Assert.Equal(CompletionItemKind.Snippet, completionItem.Kind);
    }
}
