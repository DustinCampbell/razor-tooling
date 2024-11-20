// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
///  Struct that holds onto either a dictionary or a <see cref="MetadataCollection"/> for
///  a tag helper builder object.
/// </summary>
internal struct MetadataHolder
{
    private Dictionary<string, string?>? _metadataDictionary;
    private MetadataCollection? _metadataCollection;

    public IDictionary<string, string?> MetadataDictionary
    {
        get
        {
            if (_metadataCollection is not null)
            {
                ThrowMixedMetadataException();
            }

            return _metadataDictionary ??= new Dictionary<string, string?>(StringComparer.Ordinal);
        }
    }

    public void SetMetadataCollection(MetadataCollection metadata)
    {
        if (_metadataDictionary is { Count: > 0 })
        {
            ThrowMixedMetadataException();
        }

        _metadataCollection = metadata;
    }

    [DoesNotReturn]
    private static void ThrowMixedMetadataException()
    {
        throw new InvalidOperationException(
            Resources.Format0_and_1_cannot_both_be_used_for_a_single_builder(nameof(SetMetadataCollection), nameof(MetadataDictionary)));
    }

    public readonly bool TryGetMetadataValue(string key, [NotNullWhen(true)] out string? value)
    {
        if (_metadataCollection is { } metadataCollection)
        {
            return metadataCollection.TryGetValue(key, out value);
        }

        if (_metadataDictionary is { } metadataDictionary)
        {
            return metadataDictionary.TryGetValue(key, out value);
        }

        value = null;
        return false;
    }

    public void Clear()
    {
        _metadataDictionary?.Clear();
        _metadataCollection = null;
    }

    public readonly MetadataCollection GetMetadataCollection()
        => _metadataCollection ?? MetadataCollection.CreateOrEmpty(_metadataDictionary);
}
