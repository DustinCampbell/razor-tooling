// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Razor.VsLspFactory;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

internal static class RangeExtensions
{
    public static readonly Range UndefinedRange = CreateRange(-1, -1, -1, -1);

    public static bool IntersectsOrTouches(this Range range, Range other)
    {
        if (range.IsBefore(other))
        {
            return false;
        }

        if (range.IsAfter(other))
        {
            return false;
        }

        return true;
    }

    private static bool IsBefore(this Range range, Range other) =>
        range.End.Line < other.Start.Line || range.End.Line == other.Start.Line && range.End.Character < other.Start.Character;

    private static bool IsAfter(this Range range, Range other) =>
        other.End.Line < range.Start.Line || other.End.Line == range.Start.Line && other.End.Character < range.Start.Character;

    public static bool OverlapsWith(this Range range, Range other)
    {
        ArgHelper.ThrowIfNull(range);
        ArgHelper.ThrowIfNull(other);

        return range.ToLinePositionSpan().OverlapsWith(other.ToLinePositionSpan());
    }

    public static bool LineOverlapsWith(this Range range, Range other)
    {
        ArgHelper.ThrowIfNull(range);
        ArgHelper.ThrowIfNull(other);

        var overlapStart = range.Start.Line;
        if (range.Start.Line.CompareTo(other.Start.Line) < 0)
        {
            overlapStart = other.Start.Line;
        }

        var overlapEnd = range.End.Line;
        if (range.End.Line.CompareTo(other.End.Line) > 0)
        {
            overlapEnd = other.End.Line;
        }

        return overlapStart.CompareTo(overlapEnd) <= 0;
    }

    public static bool Contains(this Range range, Range other)
    {
        ArgHelper.ThrowIfNull(range);
        ArgHelper.ThrowIfNull(other);

        return range.Start.CompareTo(other.Start) <= 0 && range.End.CompareTo(other.End) >= 0;
    }

    public static bool SpansMultipleLines(this Range range)
    {
        ArgHelper.ThrowIfNull(range);

        return range.Start.Line != range.End.Line;
    }

    public static TextSpan ToTextSpan(this Range range, SourceText sourceText)
        => sourceText.GetTextSpan(range.Start.Line, range.Start.Character, range.End.Line, range.End.Character);

    public static LinePositionSpan ToLinePositionSpan(this Range range)
        => new(range.Start.ToLinePosition(), range.End.ToLinePosition());

    public static bool IsUndefined(this Range range)
    {
        ArgHelper.ThrowIfNull(range);

        return range == UndefinedRange;
    }

    public static int CompareTo(this Range range1, Range range2)
    {
        var result = range1.Start.CompareTo(range2.Start);

        if (result == 0)
        {
            result = range1.End.CompareTo(range2.End);
        }

        return result;
    }

    public static string ToDisplayString(this Range range)
    {
        return $"({range.Start.Line}, {range.Start.Character})-({range.End.Line}, {range.End.Character})";
    }

    public static TextSpan AsTextSpan(this Range range, SourceText text)
    {
        ArgHelper.ThrowIfNull(range);
        ArgHelper.ThrowIfNull(text);

        var start = text.Lines[range.Start.Line].Start + range.Start.Character;
        var end = text.Lines[range.End.Line].Start + range.End.Character;
        return new TextSpan(start, end - start);
    }

    public static Range? Overlap(this Range range, Range other)
    {
        ArgHelper.ThrowIfNull(range);
        ArgHelper.ThrowIfNull(other);

        var overlapStart = range.Start;
        if (range.Start.CompareTo(other.Start) < 0)
        {
            overlapStart = other.Start;
        }

        var overlapEnd = range.End;
        if (range.End.CompareTo(other.End) > 0)
        {
            overlapEnd = other.End;
        }

        // Empty ranges do not overlap with any range.
        if (overlapStart.CompareTo(overlapEnd) < 0)
        {
            return CreateRange(overlapStart, overlapEnd);
        }

        return null;
    }
}
