// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal abstract partial class AbstractTagHelperCompletionService
{
    protected sealed class CompletionBuilder<T>
        where T : TagHelperObject<T>
    {
        private static readonly ObjectPool<ItemsBuilder> s_itemBuilderPool = DefaultPool.Create(Policy.Instance);

        private sealed class Policy : IPooledObjectPolicy<ItemsBuilder>
        {
            public static readonly Policy Instance = new();

            private Policy()
            {
            }

            public ItemsBuilder Create() => new();

            public bool Return(ItemsBuilder builder)
            {
                builder.Clear();
                return true;
            }
        }

        private sealed class ItemsBuilder
        {
            private readonly HashSet<T> _set = [];
            private readonly ImmutableArray<T>.Builder _builder = ImmutableArray.CreateBuilder<T>();

            public void Add(T item)
            {
                if (_set.Add(item))
                {
                    _builder.Add(item);
                }
            }

            public void Clear()
            {
                _set.Clear();
                _builder.Clear();
            }

            public ImmutableArray<T> ToImmutable()
                => _builder.ToImmutable();
        }

        private readonly ImmutableDictionary<string, ItemsBuilder>.Builder _mapBuilder
            = ImmutableDictionary.CreateBuilder<string, ItemsBuilder>(StringComparer.OrdinalIgnoreCase);
        private readonly ImmutableDictionary<string, ImmutableArray<T>>.Builder _resultBuilder
            = ImmutableDictionary.CreateBuilder<string, ImmutableArray<T>>(StringComparer.OrdinalIgnoreCase);

        public void Add(string key)
        {
            if (!_mapBuilder.ContainsKey(key))
            {
                _mapBuilder.Add(key, s_itemBuilderPool.Get());
            }
        }

        public void Add(string key, T value)
        {
            if (!_mapBuilder.TryGetValue(key, out var itemsBuilder))
            {
                itemsBuilder = s_itemBuilderPool.Get();
                _mapBuilder.Add(key, itemsBuilder);
            }

            itemsBuilder.Add(value);
        }

        public void Clear()
        {
            foreach (var (_, itemsBuilder) in _mapBuilder)
            {
                s_itemBuilderPool.Return(itemsBuilder);
            }

            _mapBuilder.Clear();
            _resultBuilder.Clear();
        }

        public ImmutableDictionary<string, ImmutableArray<T>> ToImmutable()
        {
            _resultBuilder.Clear();

            foreach (var (key, value) in _mapBuilder)
            {
                _resultBuilder.Add(key, value.ToImmutable());
            }

            return _resultBuilder.ToImmutable();
        }
    }
}
