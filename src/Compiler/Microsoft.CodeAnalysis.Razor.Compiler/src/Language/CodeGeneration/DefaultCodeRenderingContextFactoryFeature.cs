// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

internal sealed class DefaultCodeRenderingContextFactoryFeature : RazorEngineFeatureBase, ICodeRenderingContextFactoryFeature
{
    public CodeRenderingContext Create(
        IntermediateNodeWriter nodeWriter,
        RazorSourceDocument sourceDocument,
        DocumentIntermediateNode documentNode,
        RazorCodeGenerationOptions options)
        => new(nodeWriter, sourceDocument, documentNode, options);
}
