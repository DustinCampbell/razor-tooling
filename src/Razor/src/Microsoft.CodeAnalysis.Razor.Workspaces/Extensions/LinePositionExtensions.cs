// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using static Microsoft.CodeAnalysis.Razor.VsLspFactory;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

internal static class LinePositionExtensions
{
    public static Position GetStartPosition(this LinePosition linePosition)
        => CreatePosition(linePosition.Line, character: 0);

    public static Position ToPosition(this LinePosition linePosition)
        => CreatePosition(linePosition.Line, linePosition.Character);

    public static bool TryGetAbsoluteIndex(this LinePosition position, SourceText sourceText, ILogger logger, out int absoluteIndex)
        => sourceText.TryGetAbsoluteIndex(position.Line, position.Character, logger, out absoluteIndex);

    public static int GetRequiredAbsoluteIndex(this LinePosition position, SourceText sourceText, ILogger? logger = null)
        => sourceText.GetRequiredAbsoluteIndex(position.Line, position.Character, logger);
}
