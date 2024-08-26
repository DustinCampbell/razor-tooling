// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public static class DocumentIntermediateNodeExtensions
{
    public static ClassDeclarationIntermediateNode FindPrimaryClass(this DocumentIntermediateNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        return FindWithAnnotation<ClassDeclarationIntermediateNode>(node, CommonAnnotations.PrimaryClass);
    }

    public static MethodDeclarationIntermediateNode FindPrimaryMethod(this DocumentIntermediateNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        return FindWithAnnotation<MethodDeclarationIntermediateNode>(node, CommonAnnotations.PrimaryMethod);
    }

    public static NamespaceDeclarationIntermediateNode FindPrimaryNamespace(this DocumentIntermediateNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        return FindWithAnnotation<NamespaceDeclarationIntermediateNode>(node, CommonAnnotations.PrimaryNamespace);
    }

    public static ImmutableArray<IntermediateNodeReference> FindDirectiveReferences(this DocumentIntermediateNode node, DirectiveDescriptor directive)
    {
        ArgHelper.ThrowIfNull(node);
        ArgHelper.ThrowIfNull(directive);

        using var visitor = new DirectiveVisitor(directive);
        visitor.Visit(node);
        return visitor.Directives.DrainToImmutable();
    }

    public static ImmutableArray<IntermediateNodeReference> FindDescendantReferences<TNode>(this DocumentIntermediateNode document)
        where TNode : IntermediateNode
    {
        ArgHelper.ThrowIfNull(document);

        using var visitor = new ReferenceVisitor<TNode>();
        visitor.Visit(document);

        return visitor.References.DrainToImmutable();
    }

    private static T FindWithAnnotation<T>(IntermediateNode node, object annotation) where T : IntermediateNode
    {
        if (node is T target && object.ReferenceEquals(target.Annotations[annotation], annotation))
        {
            return target;
        }

        for (var i = 0; i < node.Children.Count; i++)
        {
            var result = FindWithAnnotation<T>(node.Children[i], annotation);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private sealed class DirectiveVisitor(DirectiveDescriptor directive) : IntermediateNodeWalker, IDisposable
    {
        private readonly DirectiveDescriptor _directive = directive;

        public PooledArrayBuilder<IntermediateNodeReference> Directives = [];

        public void Dispose()
        {
            Directives.Dispose();
        }

        public override void VisitDirective(DirectiveIntermediateNode node)
        {
            if (_directive == node.Directive)
            {
                Directives.Add(new IntermediateNodeReference(Parent, node));
            }

            base.VisitDirective(node);
        }
    }

    private sealed class ReferenceVisitor<TNode> : IntermediateNodeWalker, IDisposable
        where TNode : IntermediateNode
    {
        public PooledArrayBuilder<IntermediateNodeReference> References = [];

        public void Dispose()
        {
            References.Dispose();
        }

        public override void VisitDefault(IntermediateNode node)
        {
            base.VisitDefault(node);

            // Use a post-order traversal because references are used to replace nodes, and thus
            // change the parent nodes.
            //
            // This ensures that we always operate on the leaf nodes first.
            if (node is TNode)
            {
                References.Add(new IntermediateNodeReference(Parent, node));
            }
        }
    }
}
