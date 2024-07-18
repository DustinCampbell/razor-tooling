// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Razor.VsLspFactory;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

using Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class RazorSyntaxNodeExtensions
{
    internal static bool IsUsingDirective(this SyntaxNode node, [NotNullWhen(true)] out SyntaxList<SyntaxNode>? children)
    {
        // Using directives are weird, because the directive keyword ("using") is part of the C# statement it represents
        if (node is RazorDirectiveSyntax razorDirective &&
            razorDirective.DirectiveDescriptor is null &&
            razorDirective.Body is RazorDirectiveBodySyntax body &&
            body.Keyword is CSharpStatementLiteralSyntax literal &&
            literal.LiteralTokens.Count > 0)
        {
            if (literal.LiteralTokens[0] is { Kind: SyntaxKind.Keyword, Content: "using" })
            {
                children = literal.LiteralTokens;
                return true;
            }
        }

        children = null;
        return false;
    }

    /// <summary>
    /// Walks up the tree through the <paramref name="owner"/>'s parents to find the outermost node that starts at the same position.
    /// </summary>
    internal static SyntaxNode? GetOutermostNode(this SyntaxNode owner)
    {
        var node = owner.Parent;
        if (node is null)
        {
            return owner;
        }

        var lastNode = node;
        while (node.SpanStart == owner.SpanStart)
        {
            lastNode = node;
            node = node.Parent;
            if (node is null)
            {
                break;
            }
        }

        return lastNode;
    }

    internal static bool TryGetPreviousSibling(this SyntaxNode syntaxNode, [NotNullWhen(true)] out SyntaxNode? previousSibling)
    {
        var syntaxNodeParent = syntaxNode.Parent;
        if (syntaxNodeParent is null)
        {
            previousSibling = default;
            return false;
        }

        var nodes = syntaxNodeParent.ChildNodes();
        for (var i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == syntaxNode)
            {
                if (i == 0)
                {
                    previousSibling = default;
                    return false;
                }
                else
                {
                    previousSibling = nodes[i - 1];
                    return true;
                }
            }
        }

        previousSibling = default;
        return false;
    }

    public static bool ContainsOnlyWhitespace(this SyntaxNode node, bool includingNewLines = true)
    {
        ArgHelper.ThrowIfNull(node);

        var tokens = node.GetTokens();

        foreach (var token in tokens)
        {
            var tokenKind = token.Kind;
            if (tokenKind != SyntaxKind.Whitespace && (tokenKind != SyntaxKind.NewLine || !includingNewLines))
            {
                return false;
            }
        }

        // All tokens were either whitespace or newlines.
        return true;
    }

    public static LinePositionSpan GetLinePositionSpan(this SyntaxNode node, RazorSourceDocument sourceDocument)
    {
        ArgHelper.ThrowIfNull(node);
        ArgHelper.ThrowIfNull(sourceDocument);

        var start = node.Position;
        var end = node.EndPosition;
        var sourceText = sourceDocument.Text;

        Debug.Assert(start <= sourceText.Length && end <= sourceText.Length, "Node position exceeds source length.");

        if (start == sourceText.Length && node.FullWidth == 0)
        {
            // Marker symbol at the end of the document.
            var location = node.GetSourceLocation(sourceDocument);
            var position = GetLinePosition(location);
            return new LinePositionSpan(position, position);
        }

        var startPosition = sourceText.Lines.GetLinePosition(start);
        var endPosition = sourceText.Lines.GetLinePosition(end);

        return new LinePositionSpan(startPosition, endPosition);

        static LinePosition GetLinePosition(SourceLocation location)
        {
            return new LinePosition(location.LineIndex, location.CharacterIndex);
        }
    }

    public static Range GetRange(this SyntaxNode node, RazorSourceDocument sourceDocument)
    {
        ArgHelper.ThrowIfNull(node);
        ArgHelper.ThrowIfNull(sourceDocument);

        var lineSpan = node.GetLinePositionSpan(sourceDocument);
        return lineSpan.ToRange();
    }

    public static Range? GetRangeWithoutWhitespace(this SyntaxNode node, RazorSourceDocument sourceDocument)
    {
        ArgHelper.ThrowIfNull(node);
        ArgHelper.ThrowIfNull(sourceDocument);

        var tokens = node.GetTokens();

        SyntaxToken? firstToken = null;
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (!token.IsWhitespace())
            {
                firstToken = token;
                break;
            }
        }

        SyntaxToken? lastToken = null;
        for (var i = tokens.Count - 1; i >= 0; i--)
        {
            var token = tokens[i];
            if (!token.IsWhitespace())
            {
                lastToken = token;
                break;
            }
        }

        if (firstToken is null && lastToken is null)
        {
            return null;
        }

        var startPositionSpan = GetLinePositionSpan(firstToken, sourceDocument, node.SpanStart);
        var endPositionSpan = GetLinePositionSpan(lastToken, sourceDocument, node.SpanStart);

        return CreateRange(startPositionSpan.Start.ToPosition(), endPositionSpan.End.ToPosition());

        // This is needed because SyntaxToken positions taken from GetTokens
        // are relative to their parent node and not to the document.
        static LinePositionSpan GetLinePositionSpan(SyntaxNode? node, RazorSourceDocument sourceDocument, int parentStart)
        {
            ArgHelper.ThrowIfNull(node);
            ArgHelper.ThrowIfNull(sourceDocument);

            var sourceText = sourceDocument.Text;

            var start = node.Position + parentStart;
            var end = node.EndPosition + parentStart;

            if (start == sourceText.Length && node.FullWidth == 0)
            {
                // Marker symbol at the end of the document.
                var location = node.GetSourceLocation(sourceDocument);
                var position = GetLinePosition(location);
                return new LinePositionSpan(position, position);
            }

            var startPosition = sourceText.Lines.GetLinePosition(start);
            var endPosition = sourceText.Lines.GetLinePosition(end);

            return new LinePositionSpan(startPosition, endPosition);

            static LinePosition GetLinePosition(SourceLocation location)
            {
                return new LinePosition(location.LineIndex, location.CharacterIndex);
            }
        }
    }

    /// <summary>
    /// Finds the innermost SyntaxNode for a given location in source, within a given node.
    /// </summary>
    /// <param name="node">The parent node to search inside.</param>
    /// <param name="index">The location to find the innermost node at.</param>
    /// <param name="includeWhitespace">Whether to include whitespace in the search.</param>
    /// <param name="walkMarkersBack">When true, if there are multiple <see cref="SyntaxKind.Marker"/> tokens in a single location, return the parent node of the
    /// first one in the tree.</param>
    public static SyntaxNode? FindInnermostNode(this SyntaxNode node, int index, bool includeWhitespace = false, bool walkMarkersBack = true)
    {
        var token = node.FindToken(index, includeWhitespace);

        // If the index is EOF but the node has index-1,
        // then try to get a token to the left of the index.
        // patterns like
        // <button></button>$$
        // should get the button node instead of the razor document (which is the parent
        // of the EOF token)
        if (token.Kind == SyntaxKind.EndOfFile && node.Span.Contains(index - 1))
        {
            token = token.GetPreviousToken(includeWhitespace);
        }

        var foundPosition = token.Position;

        if (walkMarkersBack && token.Kind == SyntaxKind.Marker)
        {
            while (true)
            {
                var previousToken = token.GetPreviousToken(includeWhitespace);

                if (previousToken.Kind != SyntaxKind.Marker || previousToken.Position != foundPosition)
                {
                    break;
                }

                token = previousToken;
            }
        }

        return token.Parent;
    }

    public static SyntaxNode? FindNode(this SyntaxNode @this, TextSpan span, bool includeWhitespace = false, bool getInnermostNodeForTie = false)
    {
        if (!@this.FullSpan.Contains(span))
        {
            throw new ArgumentOutOfRangeException(nameof(span));
        }

        var node = @this.FindToken(span.Start, includeWhitespace)
            .Parent
            !.FirstAncestorOrSelf<SyntaxNode>(a => a.FullSpan.Contains(span));

        node.AssumeNotNull();

        // Tie-breaking.
        if (!getInnermostNodeForTie)
        {
            var cuRoot = node.Ancestors();

            // Only null if node is the original node is the root
            if (cuRoot is null)
            {
                return node;
            }

            while (true)
            {
                var parent = node.Parent;
                // NOTE: We care about FullSpan equality, but FullWidth is cheaper and equivalent.
                if (parent == null || parent.FullWidth != node.FullWidth)
                {
                    break;
                }

                // prefer child over compilation unit
                if (parent == cuRoot)
                {
                    break;
                }

                node = parent;
            }
        }

        return node;
    }

    public static bool ExistsOnTarget(this SyntaxNode node, SyntaxNode target)
    {
        var nodeString = node.RemoveEmptyNewLines().ToFullString();
        var matchingNode = target.DescendantNodesAndSelf()
            // Empty new lines can affect our comparison so we remove them since they're insignificant.
            .Where(n => n.RemoveEmptyNewLines().ToFullString() == nodeString)
            .FirstOrDefault();

        return matchingNode is not null;
    }

    public static SyntaxNode RemoveEmptyNewLines(this SyntaxNode node)
    {
        if (node is MarkupTextLiteralSyntax markupTextLiteral)
        {
            var literalTokensWithoutLines = new AspNetCore.Razor.Language.Syntax.SyntaxList<SyntaxToken>(
                markupTextLiteral.LiteralTokens.Where(t => t.Kind != SyntaxKind.NewLine));
            var updatedLiteral = markupTextLiteral.WithLiteralTokens(literalTokensWithoutLines);
            return updatedLiteral;
        }

        return node;
    }

    public static bool IsCSharpNode(this SyntaxNode node, [NotNullWhen(true)] out CSharpCodeBlockSyntax? csharpCodeBlock)
    {
        csharpCodeBlock = null;

        // Any piece of C# code can potentially be surrounded by a CSharpCodeBlockSyntax.
        if (node is CSharpCodeBlockSyntax outerCSharpCodeBlock)
        {
            var innerCSharpNode = outerCSharpCodeBlock.ChildNodes().FirstOrDefault(
                n => n is CSharpStatementSyntax or
                     RazorDirectiveSyntax or
                     CSharpExplicitExpressionSyntax or
                     CSharpImplicitExpressionSyntax);
            if (innerCSharpNode is not null)
            {
                return innerCSharpNode.IsCSharpNode(out csharpCodeBlock);
            }
        }
        // @code {
        //    var foo = "bar";
        // }
        else if (node is RazorDirectiveSyntax razorDirective)
        {
            // code {
            //    var foo = "bar";
            // }
            var razorDirectiveBody = razorDirective.Body as RazorDirectiveBodySyntax;
            if (razorDirectiveBody is not null)
            {
                var directive = razorDirectiveBody.Keyword.ToFullString();
                if (directive != "code")
                {
                    return false;
                }

                // {
                //    var foo = "bar";
                // }
                csharpCodeBlock = razorDirectiveBody.CSharpCode;

                // var foo = "bar";
                var innerCodeBlock = csharpCodeBlock.ChildNodes().FirstOrDefault(n => n is CSharpCodeBlockSyntax);
                if (innerCodeBlock is not null)
                {
                    csharpCodeBlock = innerCodeBlock as CSharpCodeBlockSyntax;
                }
            }
        }
        // @(x)
        else if (node is CSharpExplicitExpressionSyntax csharpExplicitExpression)
        {
            // (x)
            var body = csharpExplicitExpression.Body as CSharpExplicitExpressionBodySyntax;
            if (body is not null)
            {
                // x
                csharpCodeBlock = body.CSharpCode;
            }
        }
        // @x
        else if (node is CSharpImplicitExpressionSyntax csharpImplicitExpression)
        {
            var csharpImplicitExpressionBody = csharpImplicitExpression.Body as CSharpImplicitExpressionBodySyntax;
            if (csharpImplicitExpressionBody is not null)
            {
                // x
                csharpCodeBlock = csharpImplicitExpressionBody.CSharpCode;
            }
        }
        // @{
        //    var x = 1;
        // }
        else if (node is CSharpStatementSyntax csharpStatement)
        {
            // {
            //    var x = 1;
            // }
            var csharpStatementBody = csharpStatement.Body;

            // var x = 1;
            csharpCodeBlock = csharpStatementBody.ChildNodes().FirstOrDefault(n => n is CSharpCodeBlockSyntax) as CSharpCodeBlockSyntax;
        }

        return csharpCodeBlock is not null;
    }
}
