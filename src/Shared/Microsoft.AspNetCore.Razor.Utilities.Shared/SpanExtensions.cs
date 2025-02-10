// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#if !NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#endif

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace System;

internal static class SpanExtensions
{
    public static unsafe void Replace(this ReadOnlySpan<char> source, Span<char> destination, char oldValue, char newValue)
    {
#if NET8_0_OR_GREATER
        source.Replace<char>(destination, oldValue, newValue);
#else
        var length = source.Length;
        if (length == 0)
        {
            return;
        }

        if (length > destination.Length)
        {
            throw new ArgumentException(SR.Destination_is_too_short, nameof(destination));
        }

        ref var src = ref MemoryMarshal.GetReference(source);
        ref var dst = ref MemoryMarshal.GetReference(destination);

        for (var i = 0; i < length; i++)
        {
            var original = Unsafe.Add(ref src, i);
            Unsafe.Add(ref dst, i) = original == oldValue ? newValue : original;
        }
#endif
    }

    public static unsafe void Replace(this Span<char> span, char oldValue, char newValue)
    {
#if NET8_0_OR_GREATER
        span.Replace<char>(oldValue, newValue);
#else
        var length = span.Length;
        if (length == 0)
        {
            return;
        }

        ref var src = ref MemoryMarshal.GetReference(span);

        for (var i = 0; i < length; i++)
        {
            ref var slot = ref Unsafe.Add(ref src, i);

            if (slot == oldValue)
            {
                slot = newValue;
            }
        }
#endif
    }

    public static ImmutableArray<TResult> SelectAsArray<T, TResult>(this ReadOnlySpan<T> source, Func<T, TResult> selector)
    {
        return source switch
        {
            [] => [],
            [var item] => [selector(item)],
            [var item1, var item2] => [selector(item1), selector(item2)],
            [var item1, var item2, var item3] => [selector(item1), selector(item2), selector(item3)],
            [var item1, var item2, var item3, var item4] => [selector(item1), selector(item2), selector(item3), selector(item4)],
            var items => BuildResult(items, selector)
        };

        static ImmutableArray<TResult> BuildResult(ReadOnlySpan<T> items, Func<T, TResult> selector)
        {
            using var results = new PooledArrayBuilder<TResult>(capacity: items.Length);

            foreach (var item in items)
            {
                results.Add(selector(item));
            }

            return results.DrainToImmutable();
        }
    }

    public static ImmutableArray<TResult> SelectAsArray<T, TArg, TResult>(this ReadOnlySpan<T> source, TArg arg, Func<T, TArg, TResult> selector)
    {
        return source switch
        {
            [] => [],
            [var item] => [selector(item, arg)],
            [var item1, var item2] => [selector(item1, arg), selector(item2, arg)],
            [var item1, var item2, var item3] => [selector(item1, arg), selector(item2, arg), selector(item3, arg)],
            [var item1, var item2, var item3, var item4] => [selector(item1, arg), selector(item2, arg), selector(item3, arg), selector(item4, arg)],
            var items => BuildResult(items, arg, selector)
        };

        static ImmutableArray<TResult> BuildResult(ReadOnlySpan<T> items, TArg arg, Func<T, TArg, TResult> selector)
        {
            using var results = new PooledArrayBuilder<TResult>(capacity: items.Length);

            foreach (var item in items)
            {
                results.Add(selector(item, arg));
            }

            return results.DrainToImmutable();
        }
    }
}
