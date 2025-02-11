// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

internal sealed class TestCodeRenderingContextFactoryFeature : RazorEngineFeatureBase, ICodeRenderingContextFactoryFeature
{
    public CodeRenderingContext Create(
        IntermediateNodeWriter nodeWriter,
        RazorSourceDocument sourceDocument,
        DocumentIntermediateNode documentNode,
        RazorCodeGenerationOptions options)
        => new TestCodeRenderingContext(nodeWriter, sourceDocument, documentNode, options);

    private sealed class TestCodeRenderingContext(
        IntermediateNodeWriter nodeWriter,
        RazorSourceDocument sourceDocument,
        DocumentIntermediateNode documentNode,
        RazorCodeGenerationOptions options)
        : CodeRenderingContext(nodeWriter, sourceDocument, documentNode, options)
    {
        public override string GetDeterministicId()
            => "__UniqueIdSuppressedForTesting__";
    }
}
