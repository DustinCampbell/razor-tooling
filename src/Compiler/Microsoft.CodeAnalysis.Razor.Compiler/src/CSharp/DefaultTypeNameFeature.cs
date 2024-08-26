// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.Razor;

internal class DefaultTypeNameFeature : TypeNameFeature
{
    public override ImmutableArray<string> ParseTypeParameters(string typeName)
    {
        ArgHelper.ThrowIfNull(typeName);

        var typeSyntax = SyntaxFactory.ParseTypeName(typeName);

        if (typeSyntax is IdentifierNameSyntax identifier)
        {
            return [];
        }

        if (TryParseCore(typeSyntax, out var result))
        {
            return result;
        }

        if (typeSyntax is not GenericNameSyntax genericName)
        {
            return [];
        }

        using var builder = new PooledArrayBuilder<string>();

        foreach (var typeArgument in genericName.TypeArgumentList.Arguments)
        {
            builder.AddRange(ParseCore(typeArgument));
        }

        return builder.DrainToImmutable();

        static bool TryParseCore(TypeSyntax typeName, out ImmutableArray<string> typeParameters)
        {
            if (typeName is ArrayTypeSyntax arrayType)
            {
                typeParameters = ParseCore(arrayType.ElementType);
                return true;
            }

            if (typeName is TupleTypeSyntax tupleType)
            {
                using var builder = new PooledArrayBuilder<string>();

                foreach (var element in tupleType.Elements)
                {
                    builder.AddRange(ParseCore(element.Type));
                }

                typeParameters = builder.ToImmutable();
                return true;
            }

            typeParameters = default;
            return false;
        }

        static ImmutableArray<string> ParseCore(TypeSyntax typeName)
        {
            // Recursively drill into arrays `T[]` and tuples `(T, T)`.
            if (TryParseCore(typeName, out var result))
            {
                return result;
            }

            // When we encounter an identifier, we assume it's a type parameter.
            if (typeName is IdentifierNameSyntax identifierName)
            {
                return [identifierName.Identifier.Text];
            }

            // Generic names like `C<T>` are ignored here because we will visit their type argument list
            // via the `.DescendantNodesAndSelf().OfType<TypeArgumentListSyntax>()` call above.
            return [];
        }
    }

    public override TypeNameRewriter CreateGenericTypeRewriter(Dictionary<string, string?> bindings)
    {
        ArgHelper.ThrowIfNull(bindings);

        return new GenericTypeNameRewriter(bindings);
    }

    public override TypeNameRewriter CreateGlobalQualifiedTypeNameRewriter(ICollection<string> ignore)
    {
        ArgHelper.ThrowIfNull(ignore);

        return new GlobalQualifiedTypeNameRewriter(ignore);
    }

    public override bool IsLambda(string expression)
    {
        var parsed = SyntaxFactory.ParseExpression(expression);
        return parsed is LambdaExpressionSyntax;
    }
}
