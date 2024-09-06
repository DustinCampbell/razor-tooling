// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class ClassifiedSpanVisitor : SyntaxWalker
{
    private readonly RazorSourceDocument _source;
    private readonly ImmutableArray<ClassifiedSpanInternal>.Builder _spans;

    private BlockKindInternal _currentBlockKind;
    private SyntaxNode? _currentBlock;

    private ClassifiedSpanVisitor(RazorSourceDocument source, ImmutableArray<ClassifiedSpanInternal>.Builder spans)
    {
        _source = source;
        _spans = spans;

        _currentBlockKind = BlockKindInternal.Markup;
    }

    public static ImmutableArray<ClassifiedSpanInternal> VisitRoot(RazorSyntaxTree syntaxTree)
    {
        using var _ = ArrayBuilderPool<ClassifiedSpanInternal>.GetPooledObject(out var builder);

        var visitor = new ClassifiedSpanVisitor(syntaxTree.Source, builder);
        visitor.Visit(syntaxTree.Root);

        return builder.DrainToImmutable();
    }

    public override void VisitRazorCommentBlock(RazorCommentBlockSyntax node)
    {
        using (CommentBlock(node))
        {
            AddSpan(node.StartCommentTransition, SpanKindInternal.Transition, AcceptedCharactersInternal.None);
            AddSpan(node.StartCommentStar, SpanKindInternal.MetaCode, AcceptedCharactersInternal.None);

            var comment = node.Comment;
            if (comment.IsMissing)
            {
                // We need to generate a classified span at this position. So insert a marker in its place.
                comment = (SyntaxToken)SyntaxFactory.Token(SyntaxKind.Marker, string.Empty).Green.CreateRed(node, node.StartCommentStar.EndPosition);
            }

            AddSpan(comment, SpanKindInternal.Comment, AcceptedCharactersInternal.Any);

            AddSpan(node.EndCommentStar, SpanKindInternal.MetaCode, AcceptedCharactersInternal.None);
            AddSpan(node.EndCommentTransition, SpanKindInternal.Transition, AcceptedCharactersInternal.None);
        }
    }

    public override void VisitCSharpCodeBlock(CSharpCodeBlockSyntax node)
    {
        if (node.Parent is CSharpStatementBodySyntax or CSharpExplicitExpressionBodySyntax or CSharpImplicitExpressionBodySyntax or RazorDirectiveBodySyntax ||
            (_currentBlockKind is BlockKindInternal.Directive && node.Children is [CSharpStatementLiteralSyntax]))
        {
            base.VisitCSharpCodeBlock(node);
            return;
        }

        using (StatementBlock(node))
        {
            base.VisitCSharpCodeBlock(node);
        }
    }

    public override void VisitCSharpStatement(CSharpStatementSyntax node)
    {
        using (StatementBlock(node))
        {
            base.VisitCSharpStatement(node);
        }
    }

    public override void VisitCSharpExplicitExpression(CSharpExplicitExpressionSyntax node)
    {
        using (ExpressionBlock(node))
        {
            base.VisitCSharpExplicitExpression(node);
        }
    }

    public override void VisitCSharpImplicitExpression(CSharpImplicitExpressionSyntax node)
    {
        using (ExpressionBlock(node))
        {
            base.VisitCSharpImplicitExpression(node);
        }
    }

    public override void VisitRazorDirective(RazorDirectiveSyntax node)
    {
        using (DirectiveBlock(node))
        {
            base.VisitRazorDirective(node);
        }
    }

    public override void VisitCSharpTemplateBlock(CSharpTemplateBlockSyntax node)
    {
        using (TemplateBlock(node))
        {
            base.VisitCSharpTemplateBlock(node);
        }
    }

    public override void VisitMarkupBlock(MarkupBlockSyntax node)
    {
        using (MarkupBlock(node))
        {
            base.VisitMarkupBlock(node);
        }
    }

    public override void VisitMarkupTagHelperAttributeValue(MarkupTagHelperAttributeValueSyntax node)
    {
        // We don't generate a classified span when the attribute value is a simple literal value.
        // This is done so we maintain the classified spans generated in 2.x which
        // used ConditionalAttributeCollapser (combines markup literal attribute values into one span with no block parent).
        if (node.Children is [MarkupDynamicAttributeValueSyntax] or { Count: > 1 })
        {
            using (MarkupBlock(node))
            {
                base.VisitMarkupTagHelperAttributeValue(node);
            }

            return;
        }

        base.VisitMarkupTagHelperAttributeValue(node);
    }

    public override void VisitMarkupStartTag(MarkupStartTagSyntax node)
    {
        using (TagBlock(node))
        {
            var children = SyntaxUtilities.GetRewrittenMarkupStartTagChildren(node, includeEditHandler: true);
            foreach (var child in children)
            {
                Visit(child);
            }
        }
    }

    public override void VisitMarkupEndTag(MarkupEndTagSyntax node)
    {
        using (TagBlock(node))
        {
            var children = SyntaxUtilities.GetRewrittenMarkupEndTagChildren(node, includeEditHandler: true);
            foreach (var child in children)
            {
                Visit(child);
            }
        }
    }

    public override void VisitMarkupTagHelperElement(MarkupTagHelperElementSyntax node)
    {
        using (TagBlock(node))
        {
            base.VisitMarkupTagHelperElement(node);
        }
    }

    public override void VisitMarkupTagHelperStartTag(MarkupTagHelperStartTagSyntax node)
    {
        foreach (var child in node.Attributes)
        {
            if (child is MarkupTagHelperAttributeSyntax or MarkupTagHelperDirectiveAttributeSyntax or MarkupMinimizedTagHelperDirectiveAttributeSyntax)
            {
                Visit(child);
            }
        }
    }

    public override void VisitMarkupTagHelperEndTag(MarkupTagHelperEndTagSyntax node)
    {
        // We don't want to generate a classified span for a tag helper end tag. Do nothing.
    }

    public override void VisitMarkupAttributeBlock(MarkupAttributeBlockSyntax node)
    {
        using (MarkupBlock(node))
        {
            var equalsSyntax = SyntaxFactory.MarkupTextLiteral(new SyntaxList<SyntaxToken>(node.EqualsToken), chunkGenerator: null);
            var mergedAttributePrefix = SyntaxUtilities.MergeTextLiterals(node.NamePrefix, node.Name, node.NameSuffix, equalsSyntax, node.ValuePrefix);
            Visit(mergedAttributePrefix);
            Visit(node.Value);
            Visit(node.ValueSuffix);
        }
    }

    public override void VisitMarkupTagHelperAttribute(MarkupTagHelperAttributeSyntax node)
    {
        Visit(node.Value);
    }

    public override void VisitMarkupTagHelperDirectiveAttribute(MarkupTagHelperDirectiveAttributeSyntax node)
    {
        Visit(node.Transition);
        Visit(node.Colon);
        Visit(node.Value);
    }

    public override void VisitMarkupMinimizedTagHelperDirectiveAttribute(MarkupMinimizedTagHelperDirectiveAttributeSyntax node)
    {
        Visit(node.Transition);
        Visit(node.Colon);
    }

    public override void VisitMarkupMinimizedAttributeBlock(MarkupMinimizedAttributeBlockSyntax node)
    {
        using (MarkupBlock(node))
        {
            var mergedAttributePrefix = SyntaxUtilities.MergeTextLiterals(node.NamePrefix, node.Name);
            Visit(mergedAttributePrefix);
        }
    }

    public override void VisitMarkupCommentBlock(MarkupCommentBlockSyntax node)
    {
        using (HtmlCommentBlock(node))
        {
            base.VisitMarkupCommentBlock(node);
        }
    }

    public override void VisitMarkupDynamicAttributeValue(MarkupDynamicAttributeValueSyntax node)
    {
        using (MarkupBlock(node))
        {
            base.VisitMarkupDynamicAttributeValue(node);
        }
    }

    public override void VisitRazorMetaCode(RazorMetaCodeSyntax node)
    {
        AddSpan(node, SpanKindInternal.MetaCode);
        base.VisitRazorMetaCode(node);
    }

    public override void VisitCSharpTransition(CSharpTransitionSyntax node)
    {
        AddSpan(node, SpanKindInternal.Transition);
        base.VisitCSharpTransition(node);
    }

    public override void VisitMarkupTransition(MarkupTransitionSyntax node)
    {
        AddSpan(node, SpanKindInternal.Transition);
        base.VisitMarkupTransition(node);
    }

    public override void VisitCSharpStatementLiteral(CSharpStatementLiteralSyntax node)
    {
        AddSpan(node, SpanKindInternal.Code);
        base.VisitCSharpStatementLiteral(node);
    }

    public override void VisitCSharpExpressionLiteral(CSharpExpressionLiteralSyntax node)
    {
        AddSpan(node, SpanKindInternal.Code);
        base.VisitCSharpExpressionLiteral(node);
    }

    public override void VisitCSharpEphemeralTextLiteral(CSharpEphemeralTextLiteralSyntax node)
    {
        AddSpan(node, SpanKindInternal.Code);
        base.VisitCSharpEphemeralTextLiteral(node);
    }

    public override void VisitUnclassifiedTextLiteral(UnclassifiedTextLiteralSyntax node)
    {
        AddSpan(node, SpanKindInternal.None);
        base.VisitUnclassifiedTextLiteral(node);
    }

    public override void VisitMarkupLiteralAttributeValue(MarkupLiteralAttributeValueSyntax node)
    {
        AddSpan(node, SpanKindInternal.Markup);
        base.VisitMarkupLiteralAttributeValue(node);
    }

    public override void VisitMarkupTextLiteral(MarkupTextLiteralSyntax node)
    {
        if (node.Parent is MarkupLiteralAttributeValueSyntax)
        {
            base.VisitMarkupTextLiteral(node);
            return;
        }

        AddSpan(node, SpanKindInternal.Markup);
        base.VisitMarkupTextLiteral(node);
    }

    public override void VisitMarkupEphemeralTextLiteral(MarkupEphemeralTextLiteralSyntax node)
    {
        AddSpan(node, SpanKindInternal.Markup);
        base.VisitMarkupEphemeralTextLiteral(node);
    }

    private BlockSaver CommentBlock(SyntaxNode node)
        => NewBlock(node, BlockKindInternal.Comment);

    private BlockSaver DirectiveBlock(SyntaxNode node)
        => NewBlock(node, BlockKindInternal.Directive);

    private BlockSaver ExpressionBlock(SyntaxNode node)
        => NewBlock(node, BlockKindInternal.Expression);

    private BlockSaver HtmlCommentBlock(SyntaxNode node)
        => NewBlock(node, BlockKindInternal.HtmlComment);

    private BlockSaver MarkupBlock(SyntaxNode node)
        => NewBlock(node, BlockKindInternal.Markup);

    private BlockSaver StatementBlock(SyntaxNode node)
        => NewBlock(node, BlockKindInternal.Statement);

    private BlockSaver TagBlock(SyntaxNode node)
        => NewBlock(node, BlockKindInternal.Tag);

    private BlockSaver TemplateBlock(SyntaxNode node)
        => NewBlock(node, BlockKindInternal.Template);

    private BlockSaver NewBlock(SyntaxNode node, BlockKindInternal kind)
    {
        var saver = new BlockSaver(this);

        _currentBlock = node;
        _currentBlockKind = kind;

        return saver;
    }

    private readonly ref struct BlockSaver(ClassifiedSpanVisitor visitor)
    {
        private readonly SyntaxNode? _previousBlock = visitor._currentBlock;
        private readonly BlockKindInternal _previousKind = visitor._currentBlockKind;

        public void Dispose()
        {
            visitor._currentBlock = _previousBlock;
            visitor._currentBlockKind = _previousKind;
        }
    }

    private void AddSpan(SyntaxNode node, SpanKindInternal kind, AcceptedCharactersInternal? acceptedCharacters = null)
    {
        if (node.IsMissing)
        {
            return;
        }

        var nodeSpan = node.GetSourceSpan(_source);
        var blackSpan = _currentBlock.GetSourceSpan(_source);

        if (acceptedCharacters is not AcceptedCharactersInternal acceptedCharactersValue)
        {
            acceptedCharactersValue = node.GetEditHandler() is SpanEditHandler context
                ? context.AcceptedCharacters
                : AcceptedCharactersInternal.Any;
        }

        _spans.Add(new(nodeSpan, blackSpan, kind, _currentBlockKind, acceptedCharactersValue));
    }
}
