// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class DefaultRazorOptimizationPhase : RazorEnginePhaseBase, IRazorOptimizationPhase
{
    public ImmutableArray<IRazorOptimizationPass> Passes { get; private set; }

    protected override void OnInitialized()
    {
        Passes = [.. ProjectEngine.Features.OfType<IRazorOptimizationPass>().OrderBy(p => p.Order)];
    }

    protected override void ExecuteCore(RazorCodeDocument codeDocument)
    {
        var documentNode = codeDocument.GetDocumentIntermediateNode();
        ThrowForMissingDocumentDependency(documentNode);

        foreach (var pass in Passes)
        {
            pass.Execute(codeDocument, documentNode);
        }

        codeDocument.SetDocumentIntermediateNode(documentNode);
    }
}
