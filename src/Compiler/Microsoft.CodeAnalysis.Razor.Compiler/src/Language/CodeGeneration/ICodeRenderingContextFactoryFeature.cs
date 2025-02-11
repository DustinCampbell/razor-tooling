// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

/// <summary>
///  Abstract factory that creates CodeRenderingContext. This allows tests to replace default CodeRenderingContext
///  with a version that is deterministic.
/// </summary>
internal interface ICodeRenderingContextFactoryFeature : IRazorEngineFeature
{
    CodeRenderingContext Create(
        IntermediateNodeWriter nodeWriter,
        RazorSourceDocument sourceDocument,
        DocumentIntermediateNode documentNode,
        RazorCodeGenerationOptions options);
}
