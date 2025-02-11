// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultRazorCSharpLoweringPhase : RazorEnginePhaseBase, IRazorCSharpLoweringPhase
{
    protected override void ExecuteCore(RazorCodeDocument codeDocument, CancellationToken cancellationToken)
    {
        var documentNode = codeDocument.GetDocumentIntermediateNode();
        ThrowForMissingDocumentDependency(documentNode);

        var target = documentNode.Target;
        if (target == null)
        {
            var message = Resources.FormatDocumentMissingTarget(
                documentNode.DocumentKind,
                nameof(CodeTarget),
                nameof(DocumentIntermediateNode.Target));
            throw new InvalidOperationException(message);
        }

        if (!Engine.TryGetFeature<ICodeRenderingContextFactoryFeature>(out var contextFactory))
        {
            contextFactory = new DefaultCodeRenderingContextFactoryFeature();
        }

        var csharpDocument = DocumentWriter.WriteDocument(codeDocument, documentNode, documentNode.Target, documentNode.Options);
        codeDocument.SetCSharpDocument(csharpDocument);
    }
}
