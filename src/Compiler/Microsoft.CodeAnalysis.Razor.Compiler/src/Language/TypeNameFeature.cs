// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.Language;

// Razor.Language doesn't reference Microsoft.CodeAnalysis.CSharp so we
// need some indirection.
internal sealed class TypeNameFeature : RazorEngineFeatureBase
{
    public ImmutableArray<string> ParseTypeParameters(string typeName)
    {
        ArgHelper.ThrowIfNull(typeName);

        var parsedTypeSyntax = SyntaxFactory.ParseTypeName(typeName);

        if (parsedTypeSyntax is IdentifierNameSyntax)
        {
            return [];
        }

        using var results = new PooledArrayBuilder<string>();
        using PooledArrayBuilder<TypeSyntax> stack = [parsedTypeSyntax];

        while (stack.TryPop(out var type))
        {
            switch (type)
            {
                case GenericNameSyntax genericName:
                    foreach (var typeArgument in genericName.TypeArgumentList.Arguments)
                    {
                        stack.Push(typeArgument);
                    }

                    break;

                case QualifiedNameSyntax qualifiedName:
                    stack.Push(qualifiedName.Right);
                    break;

                case ArrayTypeSyntax arrayType:
                    stack.Push(arrayType.ElementType);
                    break;

                case PointerTypeSyntax pointerType:
                    stack.Push(pointerType.ElementType);
                    break;

                case NullableTypeSyntax nullableType:
                    stack.Push(nullableType.ElementType);
                    break;

                case TupleTypeSyntax tupleType:
                    foreach (var element in tupleType.Elements)
                    {
                        stack.Push(element.Type);
                    }

                    break;

                case IdentifierNameSyntax { Parent: not QualifiedNameSyntax } identifierName:
                    results.Add(identifierName.Identifier.Text);
                    break;
            }
        }

        return results.DrainToImmutable();
    }

    public TypeNameRewriter CreateGenericTypeRewriter(Dictionary<string, string?> bindings)
    {
        ArgHelper.ThrowIfNull(bindings);

        return new GenericTypeNameRewriter(bindings);
    }

    public TypeNameRewriter CreateGlobalQualifiedTypeNameRewriter(ICollection<string> ignore)
    {
        ArgHelper.ThrowIfNull(ignore);

        return new GlobalQualifiedTypeNameRewriter(ignore);
    }

    public bool IsLambda(string expression)
    {
        var parsed = SyntaxFactory.ParseExpression(expression);
        return parsed is LambdaExpressionSyntax;
    }
}
