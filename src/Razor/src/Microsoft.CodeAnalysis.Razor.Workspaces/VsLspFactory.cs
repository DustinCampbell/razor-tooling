// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.CodeAnalysis.Razor;

/// <summary>
///  Helper methods and extensions for working with Visual Studio Protocol type.
/// </summary>
internal static class VsLspFactory
{
    /// <summary>
    ///  Creates a <see cref="Position"/> from a line and character.
    /// </summary>
    public static Position CreatePosition(int line, int character)
        => new() { Line = line, Character = character };

    /// <summary>
    ///  Creates a <see cref="Position"/> from a line and character pair.
    /// </summary>
    public static Position CreatePosition((int line, int character) pair)
        => new() { Line = pair.line, Character = pair.character };

    /// <summary>
    ///  Creates a <see cref="Position"/> from this <see cref="SourceLocation"/>.
    /// </summary>
    public static Position ToPosition(this SourceLocation location)
        => new() { Line = location.LineIndex, Character = location.CharacterIndex };

    /// <summary>
    ///  Creates an empty <see cref="Position"/>; i.e. a <see cref="Position"/>
    ///  with zero for both the line character.
    /// </summary>
    public static Position EmptyPosition()
        => CreatePosition(0, 0);

    /// <summary>
    ///  Determines whether this <see cref="Position"/> is empty.
    /// </summary>
    public static bool IsEmpty(this Position position)
        => position.Line == 0 && position.Character == 0;

    /// <summary>
    ///  Updates this <see cref="Position"/> by applying the given update functions
    ///  to the line and character values.
    /// </summary>
    public static Position Update(this Position position, Func<int, int> updateLine, Func<int, int> updateCharacter)
        => CreatePosition(updateLine(position.Line), updateCharacter(position.Character));

    /// <summary>
    ///  Updates this <see cref="Position"/> by applying the given update function to the line value.
    /// </summary>
    public static Position UpdateLine(this Position position, Func<int, int> updateLine)
        => CreatePosition(updateLine(position.Line), position.Character);

    /// <summary>
    ///  Updates this <see cref="Position"/> by applying the given update function to the character value.
    /// </summary>
    public static Position UpdateCharacter(this Position position, Func<int, int> updateCharacter)
        => CreatePosition(position.Line, updateCharacter(position.Character));

    /// <summary>
    ///  Creates a new <see cref="Position"/> from an existing position and an updated line value.
    /// </summary>
    public static Position WithLine(this Position position, int line)
        => CreatePosition(line, position.Character);

    /// <summary>
    ///  Creates a new <see cref="Position"/> from an existing position and an updated line value.
    /// </summary>
    public static Position WithLine(this Position position, Func<int, int> updateLine)
        => CreatePosition(updateLine(position.Line), position.Character);

    /// <summary>
    ///  Creates a new <see cref="Position"/> from an existing position and an updated character value.
    /// </summary>
    public static Position WithCharacter(this Position position, int character)
        => CreatePosition(position.Line, character);

    /// <summary>
    ///  Creates a new <see cref="Position"/> from an existing position and an updated character value.
    /// </summary>
    public static Position WithCharacter(this Position position, Func<int, int> updateCharacter)
        => CreatePosition(position.Line, updateCharacter(position.Character));

    /// <summary>
    ///  Creates a new <see cref="Range"/> using this <see cref="Position"/> as the start and end.
    /// </summary>
    public static Range ToCollapsedRange(this Position position)
        => CreateRange(position, position);

    /// <summary>
    ///  Determines whether this <see cref="Range"/> is collapsed, that is, its start and end
    ///  positions are equal.
    /// </summary>
    public static bool IsCollapsed(this Range range)
        => range.Start == range.End;

    /// <summary>
    ///  Creates a <see cref="Range"/> from start and end positions.
    /// </summary>
    public static Range CreateRange(Position start, Position end)
        => new() { Start = start, End = end };

    /// <summary>
    ///  Creates a <see cref="Range"/> from the specified starting and ending values.
    /// </summary>
    public static Range CreateRange(int startLine, int startCharacter, int endLine, int endCharacter)
        => CreateRange(CreatePosition(startLine, startCharacter), CreatePosition(endLine, endCharacter));

    /// <summary>
    ///  Creates a <see cref="Range"/> from the specified starting and ending values.
    /// </summary>
    public static Range CreateRange((int line, int character) start, (int line, int character) end)
        => CreateRange(CreatePosition(start), CreatePosition(end));

    /// <summary>
    ///  Creates an empty <see cref="Range"/>; i.e. a <see cref="Range"/>
    ///  containing empty start and end positions.
    /// </summary>
    public static Range EmptyRange()
        => CreateRange(start: EmptyPosition(), end: EmptyPosition());

    /// <summary>
    ///  Determines whether this <see cref="Range"/> is empty.
    /// </summary>
    public static bool IsEmpty(this Range range)
        => range.Start.IsEmpty() && range.End.IsEmpty();

    /// <summary>
    ///  Creates a new <see cref="Range"/> from an existing range and an updated start <see cref="Position"/>.
    /// </summary>
    public static Range WithStart(this Range range, Position start)
        => CreateRange(start, range.End);

    /// <summary>
    ///  Creates a new <see cref="Range"/> from an existing range and an updated start <see cref="Position"/>.
    /// </summary>
    public static Range WithStart(this Range range, Func<Position, Position> updateStart)
        => CreateRange(updateStart(range.Start), range.End);

    /// <summary>
    ///  Creates a new <see cref="Range"/> from an existing range and an updated start <see cref="Position"/>,
    ///  produced from the given values.
    /// </summary>
    public static Range WithStart(this Range range, int startLine, int startCharacter)
        => CreateRange(CreatePosition(startLine, startCharacter), range.End);

    /// <summary>
    ///  Creates a new <see cref="Range"/> from an existing range and an updated start <see cref="Position"/>,
    ///  produced from the given values.
    /// </summary>
    public static Range WithStart(this Range range, (int line, int character) start)
        => CreateRange(CreatePosition(start), range.End);

    /// <summary>
    ///  Creates a new <see cref="Range"/> from an existing range and an updated end <see cref="Position"/>.
    /// </summary>
    public static Range WithEnd(this Range range, Position end)
        => CreateRange(range.Start, end);

    /// <summary>
    ///  Creates a new <see cref="Range"/> from an existing range and an updated end <see cref="Position"/>.
    /// </summary>
    public static Range WithEnd(this Range range, Func<Position, Position> updateEnd)
        => CreateRange(range.Start, updateEnd(range.End));

    /// <summary>
    ///  Creates a new <see cref="Range"/> from an existing range and an updated end <see cref="Position"/>,
    ///  produced from the given values.
    /// </summary>
    public static Range WithEnd(this Range range, int endLine, int endCharacter)
        => CreateRange(range.Start, CreatePosition(endLine, endCharacter));

    /// <summary>
    ///  Creates a new <see cref="Range"/> from an existing range and an updated end <see cref="Position"/>,
    ///  produced from the given values.
    /// </summary>
    public static Range WithEnd(this Range range, (int line, int character) end)
        => CreateRange(range.Start, CreatePosition(end));
}
