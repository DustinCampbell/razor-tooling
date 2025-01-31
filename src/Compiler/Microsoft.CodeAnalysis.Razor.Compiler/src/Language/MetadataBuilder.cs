// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

internal ref struct MetadataBuilder(int initialCapacity = 4)
{
    private MemoryBuilder<KeyValuePair<string, string?>> _builder = new(initialCapacity);

    public readonly ReadOnlySpan<KeyValuePair<string, string?>> Span => _builder.AsMemory().Span;

    public void Dispose()
    {
        _builder.Dispose();
        _builder = default;
    }

    public void Add(string key, string? value)
    {
        _builder.Append(KeyValuePair.Create(key, value));
    }

    public void Add(params ReadOnlySpan<KeyValuePair<string, string?>> pairs)
    {
        _builder.Append(pairs);
    }

    public MetadataCollection Build()
    {
        var result = MetadataCollection.Create(_builder.AsMemory().Span);

        _builder.Dispose();
        _builder = default;

        return result;
    }
}
