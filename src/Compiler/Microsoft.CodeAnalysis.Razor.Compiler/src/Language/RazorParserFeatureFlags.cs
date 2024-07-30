// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

internal readonly record struct RazorParserFeatureFlags(
    bool AllowMinimizedBooleanTagHelperAttributes,
    bool AllowHtmlCommentsInTagHelpers,
    bool AllowComponentFileKind,
    bool AllowRazorInAllCodeBlocks,
    bool AllowUsingVariableDeclarations,
    bool AllowConditionalDataDashAttributes,
    bool AllowCSharpInMarkupAttributeArea,
    bool AllowNullableForgivenessOperator)
{
    public static RazorParserFeatureFlags Create(RazorLanguageVersion version, string fileKind)
    {
        ArgHelper.ThrowIfNull(fileKind);

        var allowMinimizedBooleanTagHelperAttributes = false;
        var allowHtmlCommentsInTagHelpers = false;
        var allowComponentFileKind = false;
        var allowRazorInAllCodeBlocks = false;
        var allowUsingVariableDeclarations = false;
        var allowConditionalDataDashAttributes = false;
        var allowCSharpInMarkupAttributeArea = true;
        var allowNullableForgivenessOperator = false;

        if (version >= RazorLanguageVersion.Version_2_1)
        {
            // Added in 2.1
            allowMinimizedBooleanTagHelperAttributes = true;
            allowHtmlCommentsInTagHelpers = true;
        }

        if (version >= RazorLanguageVersion.Version_3_0)
        {
            // Added in 3.0
            allowComponentFileKind = true;
            allowRazorInAllCodeBlocks = true;
            allowUsingVariableDeclarations = true;
            allowNullableForgivenessOperator = true;
        }

        if (FileKinds.IsComponent(fileKind))
        {
            allowConditionalDataDashAttributes = true;
            allowCSharpInMarkupAttributeArea = false;
        }

        if (version >= RazorLanguageVersion.Experimental)
        {
            allowConditionalDataDashAttributes = true;
        }

        return new RazorParserFeatureFlags(
            allowMinimizedBooleanTagHelperAttributes,
            allowHtmlCommentsInTagHelpers,
            allowComponentFileKind,
            allowRazorInAllCodeBlocks,
            allowUsingVariableDeclarations,
            allowConditionalDataDashAttributes,
            allowCSharpInMarkupAttributeArea,
            allowNullableForgivenessOperator);
    }
}
