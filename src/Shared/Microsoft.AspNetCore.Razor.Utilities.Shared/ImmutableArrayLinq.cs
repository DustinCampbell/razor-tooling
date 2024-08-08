// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor;

internal static class ImmutableArrayLinq
{
    private sealed class LinqStorage<T> : IDisposable
    {
        private readonly List<PooledArray<T>> _pooledArrays = [];

        public PooledArray<T> GetNewArray(int minimumLength)
        {
            var pooledArray = ArrayPool<T>.Shared.GetPooledArray(minimumLength);
            _pooledArrays.Add(pooledArray);
            return pooledArray;
        }

        public void Dispose()
        {
            foreach (var pooledArray in _pooledArrays)
            {
                pooledArray.Dispose();
            }

            _pooledArrays.Clear();
        }
    }

    public readonly ref struct Linq<T>(ReadOnlySpan<T> span)
    {
        private readonly ReadOnlySpan<T> _span = span;

        public Linq<TResult> Select<TResult>(Func<T, TResult> selector)
        {
            Span<TResult> result = stackalloc TResult[_span.Length];

            for (var i = 0; i < _span.Length; i++)
            {
                result[i] = selector(_span[i]);
            }

            return new Linq<TResult>(result);
        }
    }
}
