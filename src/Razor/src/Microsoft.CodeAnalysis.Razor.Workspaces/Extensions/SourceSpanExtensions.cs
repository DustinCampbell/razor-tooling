// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

internal static class SourceSpanExtensions
{
    public static Range ToRange(this SourceSpan sourceSpan, SourceText text)
        => text.GetRange(sourceSpan);

    public static LinePositionSpan ToLinePositionSpan(this SourceSpan sourceSpan, SourceText text)
    {
        var (start, end) = text.GetLinesAndOffsets(sourceSpan);

        return new LinePositionSpan(new(start.lineNumber, start.offset), new(end.lineNumber, end.offset));
    }
}
