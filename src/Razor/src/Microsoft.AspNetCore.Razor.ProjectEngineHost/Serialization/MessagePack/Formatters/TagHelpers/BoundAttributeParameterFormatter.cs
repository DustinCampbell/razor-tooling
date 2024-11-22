﻿// Copyright (c) .NET Foundation. All rights reserved.
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
        reader.ReadArrayHeaderAndVerify(9);

        var kind = (TagHelperKind)reader.ReadInt32();
        var name = CachedStringFormatter.Instance.Deserialize(ref reader, options);
        var typeName = CachedStringFormatter.Instance.Deserialize(ref reader, options).AssumeNotNull();
        var propertyName = CachedStringFormatter.Instance.Deserialize(ref reader, options);
        var flags = (BoundAttributeParameterFlags)reader.ReadInt32();
        var displayName = CachedStringFormatter.Instance.Deserialize(ref reader, options).AssumeNotNull();
        var documentationObject = reader.Deserialize<DocumentationObject>(options);

        var metadata = reader.Deserialize<MetadataCollection>(options);
        var diagnostics = reader.Deserialize<ImmutableArray<RazorDiagnostic>>(options);

        return new BoundAttributeParameterDescriptor(
            kind, name!, typeName, propertyName,
            flags, documentationObject, displayName,
            metadata, diagnostics);
    }

    public override void Serialize(ref MessagePackWriter writer, BoundAttributeParameterDescriptor value, SerializerCachingOptions options)
    {
        writer.WriteArrayHeader(9);

        writer.Write((int)value.Kind);
        CachedStringFormatter.Instance.Serialize(ref writer, value.Name, options);
        CachedStringFormatter.Instance.Serialize(ref writer, value.TypeName, options);
        CachedStringFormatter.Instance.Serialize(ref writer, value.PropertyName, options);
        writer.Write((int)value.Flags);
        CachedStringFormatter.Instance.Serialize(ref writer, value.DisplayName, options);
        writer.Serialize(value.DocumentationObject, options);

        writer.Serialize(value.Metadata, options);
        writer.Serialize(value.Diagnostics, options);
    }

    public override void Skim(ref MessagePackReader reader, SerializerCachingOptions options)
    {
        reader.ReadArrayHeaderAndVerify(9);

        reader.Skip(); // Kind
        CachedStringFormatter.Instance.Skim(ref reader, options); // Name
        CachedStringFormatter.Instance.Skim(ref reader, options); // TypeName
        CachedStringFormatter.Instance.Skim(ref reader, options); // PropertyName
        reader.Skip(); // Flags
        CachedStringFormatter.Instance.Skim(ref reader, options); // DisplayName
        DocumentationObjectFormatter.Instance.Skim(ref reader, options); // DocumentationObject

        MetadataCollectionFormatter.Instance.Skim(ref reader, options); // Metadata
        RazorDiagnosticFormatter.Instance.SkimArray(ref reader, options); // Diagnostics
    }
}
