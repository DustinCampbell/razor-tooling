// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Razor.VsLspFactory;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

internal static class TextSpanExtensions
{
    internal static TextSpan TrimLeadingWhitespace(this TextSpan span, SourceText text)
    {
        ArgHelper.ThrowIfNull(text);

        for (var i = 0; i < span.Length; i++)
        {
            if (!char.IsWhiteSpace(text[span.Start + i]))
            {
                return new TextSpan(span.Start + i, span.Length - i);
            }
        }

        return span;
    }

    public static Range ToRange(this TextSpan span, SourceText text)
    {
        ArgHelper.ThrowIfNull(text);

        var (start, end) = text.GetLinesAndOffsets(span);
        return CreateRange(start, end);
    }
}
