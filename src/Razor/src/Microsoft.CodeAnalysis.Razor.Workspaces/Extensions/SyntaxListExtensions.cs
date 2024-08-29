// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class SyntaxListExtensions
{
    internal static SyntaxNode PreviousSiblingOrSelf(this SyntaxList<RazorSyntaxNode> syntaxList, RazorSyntaxNode syntaxNode)
    {
        var index = syntaxList.IndexOf(syntaxNode);

        return index switch
        {
            0 => syntaxNode,
            -1 => ThrowHelper.ThrowInvalidOperationException<SyntaxNode>("The provided node was not in the SyntaxList"),
            _ => syntaxList[index - 1]
        };
    }

    internal static SyntaxNode NextSiblingOrSelf(this SyntaxList<RazorSyntaxNode> syntaxList, RazorSyntaxNode syntaxNode)
    {
        var index = syntaxList.IndexOf(syntaxNode);

        return index switch
        {
            var i when i == syntaxList.Count - 1 => syntaxNode,
            -1 => ThrowHelper.ThrowInvalidOperationException<SyntaxNode>("The provided node was not in the SyntaxList"),
            _ => syntaxList[index + 1]
        };
    }

    internal static bool TryGetOpenBraceNode(this SyntaxList<RazorSyntaxNode> children, [NotNullWhen(true)] out RazorMetaCodeSyntax? brace)
    {
        // If there is no whitespace between the directive and the brace then there will only be
        // three children and the brace should be the first child
        brace = null;

        if (children.FirstOrDefault(static c => c.Kind == SyntaxKind.RazorMetaCode) is RazorMetaCodeSyntax metaCode)
        {
            var token = metaCode.MetaCode.SingleOrDefault(static m => m.Kind == SyntaxKind.LeftBrace);
            if (token != null)
            {
                brace = metaCode;
            }
        }

        return brace != null;
    }

    internal static bool TryGetCloseBraceNode(this SyntaxList<RazorSyntaxNode> children, [NotNullWhen(true)] out RazorMetaCodeSyntax? brace)
    {
        // If there is no whitespace between the directive and the brace then there will only be
        // three children and the brace should be the last child
        brace = null;

        if (children.LastOrDefault(static c => c.Kind == SyntaxKind.RazorMetaCode) is RazorMetaCodeSyntax metaCode)
        {
            var token = metaCode.MetaCode.SingleOrDefault(static m => m.Kind == SyntaxKind.RightBrace);
            if (token != null)
            {
                brace = metaCode;
            }
        }

        return brace != null;
    }

    internal static bool TryGetOpenBraceToken(this SyntaxList<RazorSyntaxNode> children, [NotNullWhen(true)] out SyntaxToken? brace)
    {
        brace = null;

        if (children.TryGetOpenBraceNode(out var metacode))
        {
            var token = metacode.MetaCode.SingleOrDefault(static m => m.Kind == SyntaxKind.LeftBrace);
            if (token != null)
            {
                brace = token;
            }
        }

        return brace != null;
    }

    internal static bool TryGetCloseBraceToken(this SyntaxList<RazorSyntaxNode> children, [NotNullWhen(true)] out SyntaxToken? brace)
    {
        brace = null;

        if (children.TryGetCloseBraceNode(out var metacode))
        {
            var token = metacode.MetaCode.SingleOrDefault(static m => m.Kind == SyntaxKind.RightBrace);
            if (token != null)
            {
                brace = token;
            }
        }

        return brace != null;
    }

    public static ImmutableArray<KeyValuePair<string, string>> ToAttributePairs(this SyntaxList<RazorSyntaxNode> attributeList)
    {
        using var result = new PooledArrayBuilder<KeyValuePair<string, string>>();

        foreach (var attribute in attributeList)
        {
            switch (attribute)
            {
                case MarkupTagHelperAttributeSyntax tagHelperAttribute:
                    {
                        var name = tagHelperAttribute.Name.GetContent();
                        var value = tagHelperAttribute.Value?.GetContent() ?? string.Empty;
                        result.Add(new KeyValuePair<string, string>(name, value));
                        break;
                    }

                case MarkupMinimizedTagHelperAttributeSyntax minimizedTagHelperAttribute:
                    {
                        var name = minimizedTagHelperAttribute.Name.GetContent();
                        result.Add(new KeyValuePair<string, string>(name, string.Empty));
                        break;
                    }

                case MarkupAttributeBlockSyntax markupAttribute:
                    {
                        var name = markupAttribute.Name.GetContent();
                        var value = markupAttribute.Value?.GetContent() ?? string.Empty;
                        result.Add(new KeyValuePair<string, string>(name, value));
                        break;
                    }

                case MarkupMinimizedAttributeBlockSyntax minimizedMarkupAttribute:
                    {
                        var name = minimizedMarkupAttribute.Name.GetContent();
                        result.Add(new KeyValuePair<string, string>(name, string.Empty));
                        break;
                    }

                case MarkupTagHelperDirectiveAttributeSyntax directiveAttribute:
                    {
                        var name = directiveAttribute.FullName;
                        var value = directiveAttribute.Value?.GetContent() ?? string.Empty;
                        result.Add(new KeyValuePair<string, string>(name, value));
                        break;
                    }

                case MarkupMinimizedTagHelperDirectiveAttributeSyntax minimizedDirectiveAttribute:
                    {
                        var name = minimizedDirectiveAttribute.FullName;
                        result.Add(new KeyValuePair<string, string>(name, string.Empty));
                        break;
                    }
            }
        }

        return result.DrainToImmutable();
    }
}
