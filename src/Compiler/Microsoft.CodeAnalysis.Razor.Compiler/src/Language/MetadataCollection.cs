// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
///  Generally, there are only a handful of metadata items stored on the various tag helper
///  objects. Often, it's just one or two items. To improve memory usage, MetadataCollection provides
///  multiple implementations to avoid creating a hash table.
/// </summary>
public abstract partial class MetadataCollection : IReadOnlyDictionary<string, string?>, IEquatable<MetadataCollection>
{
    public static readonly MetadataCollection Empty = NoItems.Instance;

    private Checksum? _checksum;

    protected MetadataCollection()
    {
    }

    private protected MetadataCollection(Checksum? checksum)
    {
        _checksum = checksum;
    }

    public abstract string? this[string key] { get; }

    public abstract IEnumerable<string> Keys { get; }
    public abstract IEnumerable<string?> Values { get; }
    public abstract int Count { get; }

    public abstract bool ContainsKey(string key);
    public abstract bool TryGetValue(string key, out string? value);

    public bool Contains(string key, string? value) => TryGetValue(key, out var v) && v == value;

    protected abstract KeyValuePair<string, string?> GetEntry(int index);

    public Enumerator GetEnumerator() => new(this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IEnumerator<KeyValuePair<string, string?>> IEnumerable<KeyValuePair<string, string?>>.GetEnumerator() => GetEnumerator();

    internal Checksum Checksum
        => _checksum ?? InterlockedOperations.Initialize(ref _checksum, ComputeChecksum());

    private Checksum ComputeChecksum()
    {
        var builder = new Checksum.Builder();

        foreach (var (key, value) in this)
        {
            builder.AppendData(key);
            builder.AppendData(value);
        }

        return builder.FreeAndGetChecksum();
    }

    public sealed override bool Equals(object? obj)
        => obj is MetadataCollection other &&
           Equals(other);

    public bool Equals(MetadataCollection? other)
        => other is not null &&
           Checksum.Equals(other.Checksum);

    public sealed override int GetHashCode()
        => Checksum.GetHashCode();

    public static MetadataCollection Create(KeyValuePair<string, string?> pair)
        => new OneToThreeItems(pair.Key, pair.Value);

    public static MetadataCollection Create(KeyValuePair<string, string?> pair1, KeyValuePair<string, string?> pair2)
        => new OneToThreeItems(pair1.Key, pair1.Value, pair2.Key, pair2.Value);

    public static MetadataCollection Create(KeyValuePair<string, string?> pair1, KeyValuePair<string, string?> pair2, KeyValuePair<string, string?> pair3)
        => new OneToThreeItems(pair1.Key, pair1.Value, pair2.Key, pair2.Value, pair3.Key, pair3.Value);

    public static MetadataCollection Create(params ReadOnlySpan<KeyValuePair<string, string?>> pairs)
        => pairs switch
        {
            [] => Empty,
            [var pair] => new OneToThreeItems(pair.Key, pair.Value),
            [var pair1, var pair2] => new OneToThreeItems(pair1.Key, pair1.Value, pair2.Key, pair2.Value),
            [var pair1, var pair2, var pair3] => new OneToThreeItems(pair1.Key, pair1.Value, pair2.Key, pair2.Value, pair3.Key, pair3.Value),
            _ => new FourOrMoreItems(pairs),
        };

    /// <summary>
    ///  This implementation represents an empty MetadataCollection.
    /// </summary>
    private sealed class NoItems : MetadataCollection
    {
        public static readonly NoItems Instance = new();

        private NoItems()
            : base(Checksum.Null)
        {
        }

        public override string this[string key] => throw new KeyNotFoundException(Resources.FormatThe_given_key_0_was_not_present(key));

        public override IEnumerable<string> Keys => Array.Empty<string>();
        public override IEnumerable<string> Values => Array.Empty<string>();
        public override int Count => 0;

        public override bool ContainsKey(string key) => false;

        protected override KeyValuePair<string, string?> GetEntry(int index)
            => throw new InvalidOperationException();

        public override bool TryGetValue(string key, out string value)
        {
            value = null!;
            return false;
        }
    }

    /// <summary>
    ///  This implementation represents a MetadataCollection with 1, 2, or 3 key/value pairs that are
    ///  stored in explicit fields.
    /// </summary>
    private sealed class OneToThreeItems : MetadataCollection
    {
        private readonly string _key1;
        private readonly string? _value1;

        [AllowNull]
        private readonly string _key2;
        private readonly string? _value2;

        [AllowNull]
        private readonly string _key3;
        private readonly string? _value3;

        private readonly int _count;

        private string[]? _keys;
        private string?[]? _values;

        public OneToThreeItems(string key1, string? value1, string key2, string? value2, string key3, string? value3)
        {
            if (key1 is null)
            {
                throw new ArgumentNullException(nameof(key1));
            }

            if (key2 is null)
            {
                throw new ArgumentNullException(nameof(key2));
            }

            if (key3 is null)
            {
                throw new ArgumentNullException(nameof(key3));
            }

            var compareKeys_1_2 = string.CompareOrdinal(key1, key2);

            if (compareKeys_1_2 == 0)
            {
                throw new ArgumentException(
                    Resources.FormatAn_item_with_the_same_key_has_already_been_added_Key_0(key2), nameof(key2));
            }

            var compareKeys_1_3 = string.CompareOrdinal(key1, key3);

            if (compareKeys_1_3 == 0)
            {
                throw new ArgumentException(
                    Resources.FormatAn_item_with_the_same_key_has_already_been_added_Key_0(key3), nameof(key3));
            }

            var compareKeys_2_3 = string.CompareOrdinal(key2, key3);

            if (compareKeys_2_3 == 0)
            {
                throw new ArgumentException(
                    Resources.FormatAn_item_with_the_same_key_has_already_been_added_Key_0(key3), nameof(key3));
            }

            // We ensure that the key/value pairs are assigned to their fields in sorted order to ensure
            // that MetadataCollections created with the same data in a different order are equal.

            var pair1 = (key1, value1);
            var pair2 = (key2, value2);
            var pair3 = (key3, value3);

            if (compareKeys_1_2 < 0)
            {
                if (compareKeys_1_3 < 0)
                {
                    // If key1 is less than both key2 and key3, it must go first.
                    // - slot 1: pair 1
                    (_key1, _value1) = pair1;

                    // Now that key1 is handled, we can determine the other two slots by comparing key2 and key3.
                    if (compareKeys_2_3 < 0)
                    {
                        // - slot 2: pair 2
                        // - slot 3: pair 3
                        (_key2, _value2) = pair2;
                        (_key3, _value3) = pair3;
                    }
                    else
                    {
                        // - slot 2: pair 3
                        // - slot 3: pair 2
                        (_key2, _value2) = pair3;
                        (_key3, _value3) = pair2;
                    }
                }
                else
                {
                    // If key1 is less than key2 but not key3, it must go in the middle.
                    // - slot 1: pair 3
                    // - slot 2: pair 1
                    // - slot 3: pair 2
                    (_key1, _value1) = pair3;
                    (_key2, _value2) = pair1;
                    (_key3, _value3) = pair2;
                }
            }
            else if (compareKeys_1_3 < 0)
            {
                // If key1 is less than key3 but not key2, it must go in the middle with
                // key2 in the first slot
                // - slot 1: pair 2
                // - slot 2: pair 1
                // - slot 3: pair 3
                (_key1, _value1) = pair2;
                (_key2, _value2) = pair1;
                (_key3, _value3) = pair3;
            }
            else
            {
                // Finally, key1 isn't less than key2 or key3, so it must go last.
                // - slot 3: pair 1
                (_key3, _value3) = pair1;

                // With key1 in slot 3, we can determine the other two slots by comparing key2 and key3. 
                if (compareKeys_2_3 < 0)
                {
                    // - slot 1: pair 2
                    // - slot 2: pair 3
                    (_key1, _value1) = pair2;
                    (_key2, _value2) = pair3;
                }
                else
                {
                    // - slot 1: pair 3
                    // - slot 2: pair 2
                    (_key1, _value1) = pair3;
                    (_key2, _value2) = pair2;
                }
            }

            _count = 3;
        }

        public OneToThreeItems(string key1, string? value1, string key2, string? value2)
        {
            if (key1 is null)
            {
                throw new ArgumentNullException(nameof(key1));
            }

            if (key2 is null)
            {
                throw new ArgumentNullException(nameof(key2));
            }

            var compareKeys = string.CompareOrdinal(key1, key2);

            if (compareKeys == 0)
            {
                throw new ArgumentException(
                    Resources.FormatAn_item_with_the_same_key_has_already_been_added_Key_0(key2), nameof(key2));
            }

            // We ensure that the key/value pairs are assigned to their fields in sorted order to ensure
            // that MetadataCollections created with the same data in a different order are equal.

            var pair1 = (key1, value1);
            var pair2 = (key2, value2);

            if (compareKeys < 0)
            {
                // - slot 1: pair 1
                // - slot 2: pair 2
                (_key1, _value1) = pair1;
                (_key2, _value2) = pair2;
            }
            else
            {
                // - slot 1: pair 2
                // - slot 2: pair 1
                (_key1, _value1) = pair2;
                (_key2, _value2) = pair1;
            }

            _count = 2;
        }

        public OneToThreeItems(string key, string? value)
        {
            _key1 = key ?? throw new ArgumentNullException(nameof(key));
            _value1 = value;

            _count = 1;
        }

        public override string? this[string key]
        {
            get
            {
                if (key == _key1)
                {
                    return _value1;
                }

                if (_count > 1 && key == _key2)
                {
                    return _value2;
                }

                if (_count > 2 && key == _key3)
                {
                    return _value3;
                }

                throw new KeyNotFoundException(
                    Resources.FormatThe_given_key_0_was_not_present(key));
            }
        }

        public override IEnumerable<string> Keys
        {
            get
            {
                return _keys ??= CreateKeys();

                string[] CreateKeys()
                {
                    return _count switch
                    {
                        1 => new[] { _key1 },
                        2 => new[] { _key1, _key2 },
                        3 => new[] { _key1, _key2, _key3 },
                        _ => throw new InvalidOperationException()
                    };
                }
            }
        }

        public override IEnumerable<string?> Values
        {
            get
            {
                return _values ??= CreateValues();

                string?[] CreateValues()
                {
                    return _count switch
                    {
                        1 => new[] { _value1 },
                        2 => new[] { _value1, _value2 },
                        3 => new[] { _value1, _value2, _value3 },
                        _ => throw new InvalidOperationException()
                    };
                }
            }
        }

        public override int Count => _count;

        public override bool ContainsKey(string key)
            => key == _key1 || (_count > 1 && key == _key2) || (_count > 2 && key == _key3);

        protected override KeyValuePair<string, string?> GetEntry(int index)
            => index switch
            {
                0 => new(_key1, _value1),
                1 => new(_key2, _value2),
                _ => new(_key3, _value3)
            };

        public override bool TryGetValue(string key, out string? value)
        {
            if (key == _key1)
            {
                value = _value1;
                return true;
            }

            if (_count > 1 && key == _key2)
            {
                value = _value2;
                return true;
            }

            if (_count > 2 && key == _key3)
            {
                value = _value3;
                return true;
            }

            value = null;
            return false;
        }
    }

    /// <summary>
    ///  This implementation represents a MetadataCollection with 4 or more items that are stored
    ///  in a pair of arrays. The keys are sorted so that lookup is O(log n).
    /// </summary>
    private sealed class FourOrMoreItems : MetadataCollection
    {
        private readonly string[] _keys;
        private readonly string?[] _values;

        private readonly int _count;

        public FourOrMoreItems(ReadOnlySpan<KeyValuePair<string, string?>> pairs)
        {
            var count = pairs.Length;

            // Create a sorted array of keys.
            var keys = new string[count];

            for (var i = 0; i < count; i++)
            {
                keys[i] = pairs[i].Key;
            }

            Array.Sort(keys, StringComparer.Ordinal);

            // Ensure that there are no duplicate keys.
            for (var i = 1; i < count; i++)
            {
                if (keys[i] == keys[i - 1])
                {
                    throw new ArgumentException(
                        Resources.FormatAn_item_with_the_same_key_has_already_been_added_Key_0(keys[i]), nameof(pairs));
                }
            }

            // Create an array for the values.
            var values = new string?[count];

            // Loop through our pairs and add each value at the correct index.
            for (var i = 0; i < count; i++)
            {
                var (key, value) = pairs[i];
                var index = Array.BinarySearch(keys, key, StringComparer.Ordinal);

                // We know that every key is in the array, so we can assume that index is >= 0.
                Debug.Assert(index >= 0);

                values[index] = value;
            }

            _keys = keys;
            _values = values;
            _count = count;
        }

        public override string? this[string key]
        {
            get
            {
                var index = Array.BinarySearch(_keys, key, StringComparer.Ordinal);

                return index >= 0
                    ? _values[index]
                    : throw new KeyNotFoundException(Resources.FormatThe_given_key_0_was_not_present(key));
            }
        }

        public override IEnumerable<string> Keys => _keys;
        public override IEnumerable<string?> Values => _values;

        public override int Count => _count;

        public override bool ContainsKey(string key)
        {
            var index = Array.BinarySearch(_keys, key, StringComparer.Ordinal);

            return index >= 0;
        }

        protected override KeyValuePair<string, string?> GetEntry(int index)
            => new(_keys[index], _values[index]);

        public override bool TryGetValue(string key, out string? value)
        {
            var index = Array.BinarySearch(_keys, key, StringComparer.Ordinal);

            if (index >= 0)
            {
                value = _values[index];
                return true;
            }

            value = null;
            return false;
        }
    }
}
