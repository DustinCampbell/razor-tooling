// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.Language;

[CollectionBuilder(typeof(TagHelperDescriptorCollection), nameof(Create))]
public sealed partial class TagHelperDescriptorCollection : IReadOnlyList<TagHelperDescriptor>
{
    private static readonly ObjectPool<Builder> s_builderPool = DefaultPool.Create(Policy.Instance);

    public static readonly TagHelperDescriptorCollection Empty = new(items: [], itemSet: [], itemsByAssemblyName: []);

    private readonly ImmutableArray<TagHelperDescriptor> _items;
    private readonly HashSet<TagHelperDescriptor> _itemSet;
    private readonly Dictionary<string, ImmutableArray<TagHelperDescriptor>> _itemsByAssemblyName;

    private TagHelperDescriptorCollection(
        ImmutableArray<TagHelperDescriptor> items,
        HashSet<TagHelperDescriptor> itemSet,
        Dictionary<string, ImmutableArray<TagHelperDescriptor>> itemsByAssemblyName)
    {
        _items = items;
        _itemSet = itemSet;
        _itemsByAssemblyName = itemsByAssemblyName;
    }

    public TagHelperDescriptor this[int index] => _items[index];
    public int Count => _items.Length;

    int IReadOnlyCollection<TagHelperDescriptor>.Count => _items.Length;

    TagHelperDescriptor IReadOnlyList<TagHelperDescriptor>.this[int index] => _items[index];

    public bool Contains(TagHelperDescriptor tagHelper)
        => _itemSet.Contains(tagHelper);

    public Enumerator GetEnumerator()
        => new(this);

    IEnumerator<TagHelperDescriptor> IEnumerable<TagHelperDescriptor>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public IBuilder ToBuilder()
    {
        var builder = s_builderPool.Get();
        builder.AddRange(this);

        return builder;
    }

    public static TagHelperDescriptorCollection Create(ReadOnlySpan<TagHelperDescriptor> tagHelpers)
    {
        if (tagHelpers.Length == 0)
        {
            return Empty;
        }

        using var builder = new Builder(capacity: tagHelpers.Length);

        foreach (var tagHelper in tagHelpers)
        {
            builder.Add(tagHelper);
        }

        return builder.ToCollection();
    }

    public static TagHelperDescriptorCollection Create(params TagHelperDescriptor[] tagHelpers)
        => Create(tagHelpers.AsSpan());

    public static TagHelperDescriptorCollection Create(IEnumerable<TagHelperDescriptor> tagHelpers)
    {
        if (tagHelpers is TagHelperDescriptorCollection collection)
        {
            return collection;
        }

        var capacity = 0;

        if (tagHelpers.TryGetCount(out var count))
        {
            if (count == 0)
            {
                return Empty;
            }

            capacity = count;
        }

        using var builder = new Builder(capacity);
        builder.AddRange(tagHelpers);

        return builder.ToCollection();
    }

    public static TagHelperDescriptorCollection Merge(params TagHelperDescriptorCollection[] collections)
    {
        if (collections.Length == 0)
        {
            return Empty;
        }

        if (collections.Length == 1)
        {
            return collections[0];
        }

        using var builder = collections[0].ToBuilder();

        for (var i = 1; i < collections.Length; i++)
        {
            builder.AddRange(collections[i]);
        }

        return builder.ToCollection();
    }

    public static IBuilder GetBuilder()
    {
        return s_builderPool.Get();
    }

    public static IBuilder GetBuilder(int capacity)
    {
        var builder = s_builderPool.Get();
        builder.EnsureCapacity(capacity);

        return builder;
    }

    public struct Enumerator : IEnumerator<TagHelperDescriptor>
    {
        private readonly TagHelperDescriptorCollection _collection;
        private int _index;
        private TagHelperDescriptor? _current;

        internal Enumerator(TagHelperDescriptorCollection collection)
        {
            _collection = collection;
            _index = 0;
            _current = null;
        }

        public readonly void Dispose()
        {
        }

        public readonly TagHelperDescriptor Current => _current!;

        readonly object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            var collection = _collection;
            if (_index < collection.Count)
            {
                _current = _collection[_index];
                _index++;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            _index = 0;
            _current = null;
        }
    }

    public interface IBuilder : IEnumerable<TagHelperDescriptor>, IDisposable
    {
        int Count { get; }

        void Add(TagHelperDescriptor tagHelper);
        void AddRange(IEnumerable<TagHelperDescriptor> tagHelpers);
        void AddRange(TagHelperDescriptorCollection collection);

        TagHelperDescriptorCollection ToCollection();
    }

    private sealed class Builder : IBuilder
    {
        private readonly ImmutableArray<TagHelperDescriptor>.Builder _items;
        private readonly HashSet<TagHelperDescriptor> _itemSet;
        private readonly Dictionary<string, ImmutableArray<TagHelperDescriptor>.Builder> _itemsByAssemblyName;

        public Builder(int capacity = 0)
        {
            ArgHelper.ThrowIfNegative(capacity);

            _items = ImmutableArray.CreateBuilder<TagHelperDescriptor>(capacity);

#if NET
            _itemSet = new HashSet<TagHelperDescriptor>(capacity);
#else
            _itemSet = [];
#endif

            _itemsByAssemblyName = [];
        }

        public void Dispose()
        {
            s_builderPool.Return(this);
        }

        public int Count => _items.Count;

        public void Add(TagHelperDescriptor tagHelper)
        {
            if (!_itemSet.Add(tagHelper))
            {
                return;
            }

            _items.Add(tagHelper);

            var assemblyItems = _itemsByAssemblyName.GetOrAdd(tagHelper.AssemblyName, _ => ArrayBuilderPool<TagHelperDescriptor>.Default.Get());
            assemblyItems.Add(tagHelper);
        }

        public void AddRange(IEnumerable<TagHelperDescriptor> tagHelpers)
        {
            if (tagHelpers is TagHelperDescriptorCollection collection)
            {
                AddRange(collection);
                return;
            }

            if (tagHelpers.TryGetCount(out var count) && count == 0)
            {
                return;
            }

            foreach (var tagHelper in tagHelpers)
            {
                Add(tagHelper);
            }
        }

        public void AddRange(TagHelperDescriptorCollection collection)
        {
            EnsureCapacity(_items.Count + collection._items.Length);

            foreach (var item in collection._items)
            {
                if (!_itemSet.Add(item))
                {
                    continue;
                }

                _items.Add(item);
            }

#if NET
            _itemsByAssemblyName.EnsureCapacity(_itemsByAssemblyName.Count + collection._itemsByAssemblyName.Count);
#endif

            foreach (var (assemblyName, assemblyItems) in collection._itemsByAssemblyName)
            {
                if (!_itemsByAssemblyName.ContainsKey(assemblyName))
                {
                    var builder = ArrayBuilderPool<TagHelperDescriptor>.Default.Get();
                    builder.SetCapacityIfLarger(assemblyItems.Length);
                    builder.AddRange(assemblyItems);

                    _itemsByAssemblyName.Add(assemblyName, builder);
                }
            }
        }

        public void Clear()
        {
            _items.Clear();
            _itemSet.Clear();

            foreach (var (_, builder) in _itemsByAssemblyName)
            {
                ArrayBuilderPool<TagHelperDescriptor>.Default.Return(builder);
            }

            _itemsByAssemblyName.Clear();
        }

        public void EnsureCapacity(int capacity)
        {
            if (_items.Capacity < capacity)
            {
                _items.SetCapacityIfLarger(capacity);
            }

#if NET
            _itemSet.EnsureCapacity(capacity);
#endif
        }

        public IEnumerator<TagHelperDescriptor> GetEnumerator()
        {
            foreach (var item in _items)
            {
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public TagHelperDescriptorCollection ToCollection()
        {
            if (_items.Count == 0)
            {
                return Empty;
            }

            // Since Builders are pooled, we use ToImmutable() instead of DrainToImmutable() so the
            // underlying arrays are re-used next time.
            var items = _items.ToImmutable();
            var set = new HashSet<TagHelperDescriptor>(_itemSet);

            var itemsByAssembly = new Dictionary<string, ImmutableArray<TagHelperDescriptor>>(capacity: _itemsByAssemblyName.Count);

            foreach (var (assemblyName, assemblyItems) in _itemsByAssemblyName)
            {
                itemsByAssembly.Add(assemblyName, assemblyItems.ToImmutable());
            }

            return new(items, set, itemsByAssembly);
        }
    }
}
