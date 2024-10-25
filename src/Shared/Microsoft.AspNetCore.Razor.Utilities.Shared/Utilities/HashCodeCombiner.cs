// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Razor.Utilities;

internal ref struct HashCodeCombiner
{
    private long _combinedHash64;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private HashCodeCombiner(long seed)
    {
        _combinedHash64 = seed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HashCodeCombiner Start()
        => new(0x1505L);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(HashCodeCombiner self)
        => self.CombinedHash;

    public readonly int CombinedHash
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _combinedHash64.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(int i)
    {
        _combinedHash64 = (_combinedHash64 << 5) + _combinedHash64 ^ i;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add<T>(T? o)
    {
        Add(o?.GetHashCode() ?? 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add<TValue>(TValue value, IEqualityComparer<TValue> comparer)
    {
        var hashCode = value != null ? comparer.GetHashCode(value) : 0;
        Add(hashCode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add<T>(ImmutableArray<T> array, IEqualityComparer<T> comparer)
    {
        if (array.IsDefault)
        {
            return;
        }

        foreach (var item in array)
        {
            Add(item, comparer);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add<T>(ImmutableArray<T> array)
    {
        Add(array, EqualityComparer<T>.Default);
    }
}
