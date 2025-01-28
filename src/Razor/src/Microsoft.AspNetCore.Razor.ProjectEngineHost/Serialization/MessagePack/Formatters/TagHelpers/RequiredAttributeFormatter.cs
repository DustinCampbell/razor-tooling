// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using MessagePack;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Serialization.MessagePack.Formatters.TagHelpers;

internal sealed class RequiredAttributeFormatter : ValueFormatter<RequiredAttributeDescriptor>
{
    public static readonly ValueFormatter<RequiredAttributeDescriptor> Instance = new RequiredAttributeFormatter();

    private RequiredAttributeFormatter()
    {
    }

    public override RequiredAttributeDescriptor Deserialize(ref MessagePackReader reader, SerializerCachingOptions options)
    {
        reader.ReadArrayHeaderAndVerify(6);

        var name = CachedStringFormatter.Instance.Deserialize(ref reader, options);
        var value = CachedStringFormatter.Instance.Deserialize(ref reader, options);
        var flags = (RequiredAttributeFlags)reader.ReadByte();
        var displayName = CachedStringFormatter.Instance.Deserialize(ref reader, options).AssumeNotNull();

        var metadata = reader.Deserialize<MetadataCollection>(options);
        var diagnostics = reader.Deserialize<ImmutableArray<RazorDiagnostic>>(options);

        return new RequiredAttributeDescriptor(
            name!, value, flags,
            displayName, diagnostics, metadata);
    }

    public override void Serialize(ref MessagePackWriter writer, RequiredAttributeDescriptor value, SerializerCachingOptions options)
    {
        writer.WriteArrayHeader(6);

        CachedStringFormatter.Instance.Serialize(ref writer, value.Name, options);
        CachedStringFormatter.Instance.Serialize(ref writer, value.Value, options);
        writer.Write((byte)value.Flags);
        CachedStringFormatter.Instance.Serialize(ref writer, value.DisplayName, options);

        writer.Serialize(value.Metadata, options);
        writer.Serialize(value.Diagnostics, options);
    }

    public override void Skim(ref MessagePackReader reader, SerializerCachingOptions options)
    {
        reader.ReadArrayHeaderAndVerify(6);

        CachedStringFormatter.Instance.Skim(ref reader, options); // Name
        CachedStringFormatter.Instance.Skim(ref reader, options); // Value
        reader.Skip(); // Flags
        CachedStringFormatter.Instance.Skim(ref reader, options); // DisplayName

        MetadataCollectionFormatter.Instance.Skim(ref reader, options); // Metadata
        RazorDiagnosticFormatter.Instance.SkimArray(ref reader, options); // Diagnostics
    }
}
