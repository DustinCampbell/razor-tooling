// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

[Flags]
internal enum RazorParserOptionsFlags
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
