// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public sealed partial class CodeWriter
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [InterpolatedStringHandler]
    public readonly ref struct WriteInterpolatedStringHandler
    {
        private readonly CodeWriter _writer;

        public WriteInterpolatedStringHandler(int literalLength, int formattedCount, CodeWriter writer)
        {
            _writer = writer;
        }

        public void AppendLiteral(string value)
            => _writer.WriteCore(value.AsMemory());

        public void AppendFormatted(ReadOnlyMemory<char> value)
            => _writer.WriteCore(value);

        public void AppendFormatted(string? value)
        {
            if (value is not null)
            {
                _writer.WriteCore(value.AsMemory());
            }
        }

        public void AppendFormatted(ReadOnlySpan<string> values)
            => _writer.Write(values);

        public void AppendFormatted(ReadOnlySpan<ReadOnlyMemory<char>> values)
            => _writer.Write(values);

        public void AppendFormatted<T>(T value)
        {
            if (value is null)
            {
                return;
            }

            switch (value)
            {
                case ReadOnlyMemory<char> memory:
                    _writer.WriteCore(memory);
                    break;

                case string s:
                    _writer.WriteCore(s.AsMemory());
                    break;

                default:
                    _writer.WriteCore(value.ToString().AsMemoryOrDefault());
                    break;
            }
        }
    }
}
