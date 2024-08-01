// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultDocumentClassifierPass : DocumentClassifierPassBase
{
    public override int Order => DefaultFeatureOrder;

    protected override string DocumentKind => "default";

    protected override bool IsMatch(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        return true;
    }

    protected override void OnDocumentStructureCreated(
        RazorCodeDocument codeDocument,
        NamespaceDeclarationIntermediateNode @namespace,
        ClassDeclarationIntermediateNode @class,
        MethodDeclarationIntermediateNode method)
    {
        if (Engine.TryGetFeature(out DefaultDocumentClassifierPassFeature configuration))
        {
            foreach (var configureClass in configuration.ConfigureClass)
            {
                configureClass(codeDocument, @class);
            }

            foreach (var configureNamespace in configuration.ConfigureNamespace)
            {
                configureNamespace(codeDocument, @namespace);
            }

            foreach (var configureMethod in configuration.ConfigureMethod)
            {
                configureMethod(codeDocument, @method);
            }
        }
    }
}
