// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using MessagePack;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Serialization.MessagePack.Formatters.TagHelpers;

internal sealed class BoundAttributeParameterFormatter : ValueFormatter<BoundAttributeParameterDescriptor>
{
    public static readonly ValueFormatter<BoundAttributeParameterDescriptor> Instance = new BoundAttributeParameterFormatter();

    private BoundAttributeParameterFormatter()
    {
    }

    public override BoundAttributeParameterDescriptor Deserialize(ref MessagePackReader reader, SerializerCachingOptions options)
    {
        reader.ReadArrayHeaderAndVerify(7);

        var name = CachedStringFormatter.Instance.Deserialize(ref reader, options);
        var typeName = CachedStringFormatter.Instance.Deserialize(ref reader, options).AssumeNotNull();
        var flags = (BoundAttributeParameterFlags)reader.ReadInt32();
        var displayName = CachedStringFormatter.Instance.Deserialize(ref reader, options).AssumeNotNull();
        var documentationObject = reader.Deserialize<DocumentationObject>(options);

        var metadata = reader.Deserialize<MetadataCollection>(options);
        var diagnostics = reader.Deserialize<ImmutableArray<RazorDiagnostic>>(options);

        return new BoundAttributeParameterDescriptor(
            name!, typeName, flags,
            documentationObject, displayName,
            metadata, diagnostics);
    }

    public override void Serialize(ref MessagePackWriter writer, BoundAttributeParameterDescriptor value, SerializerCachingOptions options)
    {
        writer.WriteArrayHeader(7);

        CachedStringFormatter.Instance.Serialize(ref writer, value.Name, options);
        CachedStringFormatter.Instance.Serialize(ref writer, value.TypeName, options);
        writer.Write((int)value.Flags);
        CachedStringFormatter.Instance.Serialize(ref writer, value.DisplayName, options);
        writer.Serialize(value.DocumentationObject, options);

        writer.Serialize(value.Metadata, options);
        writer.Serialize(value.Diagnostics, options);
    }

    public override void Skim(ref MessagePackReader reader, SerializerCachingOptions options)
    {
        reader.ReadArrayHeaderAndVerify(7);

        CachedStringFormatter.Instance.Skim(ref reader, options); // Name
        CachedStringFormatter.Instance.Skim(ref reader, options); // TypeName
        reader.Skip(); // Flags
        CachedStringFormatter.Instance.Skim(ref reader, options); // DisplayName
        DocumentationObjectFormatter.Instance.Skim(ref reader, options); // DocumentationObject

        MetadataCollectionFormatter.Instance.Skim(ref reader, options); // Metadata
        RazorDiagnosticFormatter.Instance.SkimArray(ref reader, options); // Diagnostics
    }
}
