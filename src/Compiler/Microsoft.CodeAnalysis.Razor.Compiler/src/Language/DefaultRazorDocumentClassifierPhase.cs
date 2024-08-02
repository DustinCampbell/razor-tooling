﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class DefaultRazorDocumentClassifierPhase : RazorEnginePhaseBase, IRazorDocumentClassifierPhase
{
    public ImmutableArray<IRazorDocumentClassifierPass> Passes { get; private set; }

    protected override void OnInitialized()
    {
        Passes = Engine.GetFeatures<IRazorDocumentClassifierPass>().OrderByAsArray(static p => p.Order);
    }

    protected override void ExecuteCore(RazorCodeDocument codeDocument)
    {
        var irDocument = codeDocument.GetDocumentIntermediateNode();
        ThrowForMissingDocumentDependency(irDocument);

        foreach (var pass in Passes)
        {
            pass.Execute(codeDocument, irDocument);
        }

        codeDocument.SetDocumentIntermediateNode(irDocument);
    }
}
