// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Razor.Diagnostics.Analyzers;

internal static class Extensions
{
    public static Diagnostic CreateDiagnostic(this SyntaxNode node, DiagnosticDescriptor rule)
        => node.GetLocation().CreateDiagnostic(rule);

    public static Diagnostic CreateDiagnostic(this IOperation operation, DiagnosticDescriptor rule)
        => operation.Syntax.CreateDiagnostic(rule);

    public static Diagnostic CreateDiagnostic(this Location location, DiagnosticDescriptor rule)
    {
        if (!location.IsInSource)
        {
            location = Location.None;
        }

        return Diagnostic.Create(descriptor: rule, location);
    }
}
