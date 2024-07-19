﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using static Microsoft.CodeAnalysis.Razor.VsLspFactory;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

internal static class SourceTextExtensions
{
    /// <summary>
    /// Gets the minimal range of text that changed between the two versions.
    /// </summary>
    public static TextChangeRange GetEncompassingTextChangeRange(this SourceText newText, SourceText oldText)
    {
        ArgHelper.ThrowIfNull(newText);
        ArgHelper.ThrowIfNull(oldText);

        var ranges = newText.GetChangeRanges(oldText);
        if (ranges.Count == 0)
        {
            return default;
        }

        // simple case.
        if (ranges.Count == 1)
        {
            return ranges[0];
        }

        return TextChangeRange.Collapse(ranges);
    }

    public static void GetLineAndOffset(this SourceText text, int position, out int lineNumber, out int offset)
    {
        ArgHelper.ThrowIfNull(text);

        var line = text.Lines.GetLineFromPosition(position);

        lineNumber = line.LineNumber;
        offset = position - line.Start;
    }

    public static (int lineNumber, int offset) GetLineAndOffset(this SourceText text, int position)
    {
        text.GetLineAndOffset(position, out var lineNumber, out var offset);
        return (lineNumber, offset);
    }

    public static ((int lineNumber, int offset) start, (int lineNumber, int offset) end) GetLinesAndOffsets(this SourceText text, TextSpan textSpan)
    {
        ArgHelper.ThrowIfNull(text);

        var start = text.GetLineAndOffset(textSpan.Start);
        var end = text.GetLineAndOffset(textSpan.End);

        return (start, end);
    }

    public static ((int lineNumber, int offset) start, (int lineNumber, int offset) end) GetLinesAndOffsets(this SourceText text, SourceSpan sourceSpan)
    {
        ArgHelper.ThrowIfNull(text);

        var start = text.GetLineAndOffset(sourceSpan.AbsoluteIndex);
        var end = text.GetLineAndOffset(sourceSpan.AbsoluteIndex + sourceSpan.Length);

        return (start, end);
    }

    public static string GetSubTextString(this SourceText text, TextSpan span)
    {
        ArgHelper.ThrowIfNull(text);

        var charBuffer = new char[span.Length];
        text.CopyTo(span.Start, charBuffer, 0, span.Length);
        return new string(charBuffer);
    }

    public static bool NonWhitespaceContentEquals(this SourceText text, SourceText otherText)
    {
        ArgHelper.ThrowIfNull(text);
        ArgHelper.ThrowIfNull(otherText);

        var i = 0;
        var j = 0;
        while (i < text.Length && j < otherText.Length)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                i++;
                continue;
            }
            else if (char.IsWhiteSpace(otherText[j]))
            {
                j++;
                continue;
            }
            else if (text[i] != otherText[j])
            {
                return false;
            }

            i++;
            j++;
        }

        while (i < text.Length && char.IsWhiteSpace(text[i]))
        {
            i++;
        }

        while (j < otherText.Length && char.IsWhiteSpace(otherText[j]))
        {
            j++;
        }

        return i == text.Length && j == otherText.Length;
    }

    public static int? GetFirstNonWhitespaceOffset(this SourceText text, TextSpan? span, out int newLineCount)
    {
        ArgHelper.ThrowIfNull(text);

        span ??= new TextSpan(0, text.Length);
        newLineCount = 0;

        for (var i = span.Value.Start; i < span.Value.End; i++)
        {
            if (!char.IsWhiteSpace(text[i]))
            {
                return i - span.Value.Start;
            }
            else if (text[i] == '\n')
            {
                newLineCount++;
            }
        }

        return null;
    }

    // Given the source text and the current span, we start at the ending span location and iterate towards the start
    // until we've reached a non-whitespace character.
    // For instance "  abcdef  " would have a last non-whitespace offset of 7 to correspond to the charcter 'f'.
    public static int? GetLastNonWhitespaceOffset(this SourceText text, TextSpan? span, out int newLineCount)
    {
        ArgHelper.ThrowIfNull(text);

        span ??= new TextSpan(0, text.Length);
        newLineCount = 0;

        // If the span is at the end of the document it's common for the "End" to represent 1 past the end of the source
        var indexableSpanEnd = Math.Min(span.Value.End, text.Length - 1);

        for (var i = indexableSpanEnd; i >= span.Value.Start; i--)
        {
            if (!char.IsWhiteSpace(text[i]))
            {
                return i - span.Value.Start;
            }
            else if (text[i] == '\n')
            {
                newLineCount++;
            }
        }

        return null;
    }

    public static bool TryGetAbsoluteIndex(this SourceText text, int line, int character, out int absoluteIndex)
    {
        return text.TryGetAbsoluteIndex(line, character, logger: null, out absoluteIndex);
    }

    public static bool TryGetAbsoluteIndex(this SourceText text, int line, int character, ILogger? logger, out int absoluteIndex)
    {
        absoluteIndex = 0;
        var lineCount = text.Lines.Count;
        if (line > lineCount ||
            (line == lineCount && character > 0))
        {
            if (logger != null)
            {
                logger?.Log(LogLevel.Error, SR.FormatPositionLine_Outside_Range(line, nameof(text), text.Lines.Count), exception: null);
                Debug.Fail(SR.FormatPositionLine_Outside_Range(line, nameof(text), text.Lines.Count));
            }

            return false;
        }

        // LSP spec allowed a Range to end one line past the end, and character 0. SourceText does not, so we adjust to the final char position
        if (line == lineCount)
        {
            absoluteIndex = text.Length;
        }
        else
        {
            var sourceLine = text.Lines[line];
            var lineLengthIncludingLineBreak = sourceLine.SpanIncludingLineBreak.Length;
            if (character > lineLengthIncludingLineBreak)
            {
                if (logger != null)
                {
                    var errorMessage = SR.FormatPositionCharacter_Outside_Range(character, nameof(text), lineLengthIncludingLineBreak);
                    logger?.Log(LogLevel.Error, errorMessage, exception: null);
                    Debug.Fail(errorMessage);
                }

                return false;
            }

            absoluteIndex = sourceLine.Start + character;
        }

        return true;
    }

    public static int GetRequiredAbsoluteIndex(this SourceText text, int line, int character, ILogger? logger = null)
    {
        if (!text.TryGetAbsoluteIndex(line, character, logger, out var absolutePosition))
        {
            throw new ArgumentOutOfRangeException($"({line},{character}) matches or exceeds SourceText boundary {text.Lines.Count}.");
        }

        return absolutePosition;
    }

    public static TextSpan GetTextSpan(this SourceText text, int startLine, int startCharacter, int endLine, int endCharacter)
    {
        ArgHelper.ThrowIfNull(text);

        var start = GetAbsoluteIndex(startLine, startCharacter, text, "Start");
        var end = GetAbsoluteIndex(endLine, endCharacter, text, "End");

        var length = end - start;
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException($"({startLine},{startCharacter})-({endLine},{endCharacter}) resolved to zero or negative length.");
        }

        return new TextSpan(start, length);

        static int GetAbsoluteIndex(int line, int character, SourceText sourceText, string argName)
        {
            if (!sourceText.TryGetAbsoluteIndex(line, character, out var absolutePosition))
            {
                throw new ArgumentOutOfRangeException($"{argName} ({line},{character}) matches or exceeds SourceText boundary {sourceText.Lines.Count}.");
            }

            return absolutePosition;
        }
    }

    public static Position GetLastPosition(this SourceText text)
        => text.GetPosition(text.Length - 1);

    public static Position GetPosition(this SourceText text, int position)
        => CreatePosition(text.GetLineAndOffset(position));

    public static Range GetRange(this SourceText text)
        => CreateRange(EmptyPosition(), text.GetLastPosition());

    public static Range GetRange(this SourceText text, int start, int end)
        => CreateRange(text.GetLineAndOffset(start), text.GetLineAndOffset(end));

    public static Range GetRange(this SourceText text, TextSpan textSpam)
    {
        var (start, end) = text.GetLinesAndOffsets(textSpam);
        return CreateRange(start, end);
    }

    public static Range GetRange(this SourceText text, SourceSpan sourceSpan)
    {
        var (start, end) = text.GetLinesAndOffsets(sourceSpan);
        return CreateRange(start, end);
    }

    public static Range GetCollapsedRange(this SourceText text, int position)
        => text.GetPosition(position).ToCollapsedRange();
}
