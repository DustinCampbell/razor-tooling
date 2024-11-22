// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// A metadata class describing a tag helper attribute.
/// </summary>
public sealed class BoundAttributeDescriptor : TagHelperObject<BoundAttributeDescriptor>
{
    private readonly BoundAttributeFlags _flags;
    private readonly DocumentationObject _documentationObject;

    public TagHelperKind Kind { get; }
    public string Name { get; }
    public string TypeName { get; }
    public string? PropertyName { get; }
    public string? GloballyQualifiedTypeName { get; }
    public string DisplayName { get; }
    public string? ContainingType { get; }

    public string? IndexerNamePrefix { get; }
    public string? IndexerTypeName { get; }

    internal BoundAttributeFlags Flags => _flags;
    public bool CaseSensitive => (_flags & BoundAttributeFlags.CaseSensitive) != 0;
    public bool HasIndexer => (_flags & BoundAttributeFlags.HasIndexer) != 0;
    public bool IsIndexerStringProperty => (_flags & BoundAttributeFlags.IsIndexerStringProperty) != 0;
    public bool IsIndexerBooleanProperty => (_flags & BoundAttributeFlags.IsIndexerBooleanProperty) != 0;
    public bool IsEnum => (_flags & BoundAttributeFlags.IsEnum) != 0;
    public bool IsStringProperty => (_flags & BoundAttributeFlags.IsStringProperty) != 0;
    public bool IsBooleanProperty => (_flags & BoundAttributeFlags.IsBooleanProperty) != 0;
    internal bool IsEditorRequired => (_flags & BoundAttributeFlags.IsEditorRequired) != 0;
    public bool IsDirectiveAttribute => (_flags & BoundAttributeFlags.IsDirectiveAttribute) != 0;
    public bool IsWeaklyTyped => (_flags & BoundAttributeFlags.IsWeaklyTyped) != 0;

    /// <summary>
    /// Gets a value that indicates whether the property is a child content property. Properties are
    /// considered child content if they have the type <c>RenderFragment</c> or <c>RenderFragment{T}</c>.
    /// </summary>
    public bool IsChildContentProperty => (_flags & BoundAttributeFlags.IsChildContentProperty) != 0;

    /// <summary>
    /// Gets a value indicating whether the attribute is of type <c>EventCallback</c> or
    /// <c>EventCallback{T}</c>
    /// </summary>
    public bool IsEventCallbackProperty => (_flags & BoundAttributeFlags.IsEventCallbackProperty) != 0;

    public ImmutableArray<BoundAttributeParameterDescriptor> Parameters { get; }
    public MetadataCollection Metadata { get; }

    internal BoundAttributeDescriptor(
        TagHelperKind kind,
        string name,
        string typeName,
        string? propertyName,
        string? globallyQualifiedTypeName,
        string? indexerNamePrefix,
        string? indexerTypeName,
        DocumentationObject documentationObject,
        string displayName,
        string? containingType,
        BoundAttributeFlags flags,
        ImmutableArray<BoundAttributeParameterDescriptor> parameters,
        MetadataCollection metadata,
        ImmutableArray<RazorDiagnostic> diagnostics)
        : base(diagnostics)
    {
        Kind = kind;
        Name = name;
        TypeName = typeName;
        PropertyName = propertyName;
        GloballyQualifiedTypeName = globallyQualifiedTypeName;
        IndexerNamePrefix = indexerNamePrefix;
        IndexerTypeName = indexerTypeName;
        _documentationObject = documentationObject;
        DisplayName = displayName;
        ContainingType = containingType;
        _flags = flags;
        Parameters = parameters.NullToEmpty();
        Metadata = metadata ?? MetadataCollection.Empty;
    }

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.AppendData((int)Kind);
        builder.AppendData(Name);
        builder.AppendData(TypeName);
        builder.AppendData(PropertyName);
        builder.AppendData(GloballyQualifiedTypeName);
        builder.AppendData(IndexerNamePrefix);
        builder.AppendData(IndexerTypeName);
        builder.AppendData(DisplayName);
        builder.AppendData(ContainingType);

        DocumentationObject.AppendToChecksum(in builder);

        builder.AppendData((int)Flags);

        foreach (var descriptor in Parameters)
        {
            builder.AppendData(descriptor.Checksum);
        }

        builder.AppendData(Metadata.Checksum);
    }

    public string? Documentation => _documentationObject.GetText();

    internal DocumentationObject DocumentationObject => _documentationObject;

    public IEnumerable<RazorDiagnostic> GetAllDiagnostics()
    {
        foreach (var parameter in Parameters)
        {
            foreach (var diagnostic in parameter.Diagnostics)
            {
                yield return diagnostic;
            }
        }

        foreach (var diagnostic in Diagnostics)
        {
            yield return diagnostic;
        }
    }

    public override string ToString()
    {
        return DisplayName ?? base.ToString()!;
    }
}
