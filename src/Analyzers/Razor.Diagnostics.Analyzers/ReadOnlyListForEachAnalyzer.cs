// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static Razor.Diagnostics.Analyzers.Resources;

namespace Razor.Diagnostics.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReadOnlyListForEachAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.ReadOnlyListForEach,
        CreateLocalizableResourceString(nameof(ReadOnlyListForEachTitle)),
        CreateLocalizableResourceString(nameof(ReadOnlyListForEachMessage)),
        DiagnosticCategory.Reliability,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateLocalizableResourceString(nameof(ReadOnlyListForEachDescription)));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(context =>
        {
            var readOnlyListOfT = context.Compilation.GetTypeByMetadataName(WellKnownTypeNames.ReadOnlyListOfT);
            if (readOnlyListOfT is null)
            {
                return;
            }

            context.RegisterSyntaxNodeAction(context => AnalyzeForEachVariable(context, readOnlyListOfT), SyntaxKind.ForEachStatement);
        });
    }

    private static void AnalyzeForEachVariable(SyntaxNodeAnalysisContext context, INamedTypeSymbol readOnlyListOfT)
    {
        var forEachStatement = (ForEachStatementSyntax)context.Node;

        var expressionTypeInfo = context.SemanticModel.GetTypeInfo(forEachStatement.Expression);
        if (expressionTypeInfo.Type is not INamedTypeSymbol expressionType || !expressionType.IsGenericType)
        {
            return;
        }

        if (!SymbolEqualityComparer.Default.Equals(expressionType.ConstructedFrom, readOnlyListOfT))
        {
            return;
        }

        context.ReportDiagnostic(forEachStatement.Expression.CreateDiagnostic(Rule));
    }
}
