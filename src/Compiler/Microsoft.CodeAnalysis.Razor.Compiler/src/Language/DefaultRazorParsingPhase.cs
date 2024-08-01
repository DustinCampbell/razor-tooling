// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class DefaultRazorParsingPhase : RazorEnginePhaseBase, IRazorParsingPhase
{
    private IRazorParserOptionsFactory? _optionsFactory;

    protected override void OnInitialized()
    {
        _optionsFactory = GetRequiredFeature<IRazorParserOptionsFactory>();
    }

    protected override void ExecuteCore(RazorCodeDocument codeDocument)
    {
        var options = codeDocument.GetParserOptions() ?? _optionsFactory.AssumeNotNull().Create();
        var syntaxTree = RazorSyntaxTree.Parse(codeDocument.Source, options);
        codeDocument.SetSyntaxTree(syntaxTree);

        using var importSyntaxTrees = new PooledArrayBuilder<RazorSyntaxTree>(codeDocument.Imports.Length);

        foreach (var import in codeDocument.Imports)
        {
            importSyntaxTrees.Add(RazorSyntaxTree.Parse(import, options));
        }

        codeDocument.SetImportSyntaxTrees(importSyntaxTrees.DrainToImmutable());
    }
}
