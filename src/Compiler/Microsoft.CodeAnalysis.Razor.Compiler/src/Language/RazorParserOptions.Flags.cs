// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial record class RazorParserOptions
{
    [Flags]
    private enum Flags
    {
        DesignTime = 1 << 0,
        ParseLeadingDirectives = 1 << 1,
        UseRoslynTokenizer = 1 << 2,
        EnableSpanEditHandlers = 1 << 3,
        AllowMinimizedBooleanTagHelperAttributes = 1 << 4,
        AllowHtmlCommentsInTagHelpers = 1 << 5,
        AllowComponentFileKind = 1 << 6,
        AllowRazorInAllCodeBlocks = 1 << 7,
        AllowUsingVariableDeclarations = 1 << 8,
        AllowConditionalDataDashAttributes = 1 << 9,
        AllowCSharpInMarkupAttributeArea = 1 << 10,
        AllowNullableForgivenessOperator = 1 << 11
    }

    private static Flags GetDefaultFlags(RazorLanguageVersion languageVersion, string fileKind)
    {
        Flags flags = 0;

        flags.SetFlag(Flags.AllowCSharpInMarkupAttributeArea);

        if (languageVersion >= RazorLanguageVersion.Version_2_1)
        {
            // Added in 2.1
            flags.SetFlag(Flags.AllowMinimizedBooleanTagHelperAttributes);
            flags.SetFlag(Flags.AllowHtmlCommentsInTagHelpers);
        }

        if (languageVersion >= RazorLanguageVersion.Version_3_0)
        {
            // Added in 3.0
            flags.SetFlag(Flags.AllowComponentFileKind);
            flags.SetFlag(Flags.AllowRazorInAllCodeBlocks);
            flags.SetFlag(Flags.AllowUsingVariableDeclarations);
            flags.SetFlag(Flags.AllowNullableForgivenessOperator);
        }

        if (FileKinds.IsComponent(fileKind))
        {
            flags.SetFlag(Flags.AllowConditionalDataDashAttributes);
            flags.ClearFlag(Flags.AllowCSharpInMarkupAttributeArea);
        }

        if (languageVersion >= RazorLanguageVersion.Experimental)
        {
            flags.SetFlag(Flags.AllowConditionalDataDashAttributes);
        }

        return flags;
    }
}
