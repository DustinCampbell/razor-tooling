// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class BoundAttributeParameterDescriptor : TagHelperObject<BoundAttributeParameterDescriptor>
{
    private readonly BoundAttributeParameterFlags _flags;
    private readonly DocumentationObject _documentationObject;

    public TagHelperKind Kind { get; }
    public string Name { get; }
    public string TypeName { get; }
    public string DisplayName { get; }

    internal BoundAttributeParameterFlags Flags => _flags;
    public bool CaseSensitive => (_flags & BoundAttributeParameterFlags.CaseSensitive) != 0;
    public bool IsEnum => (_flags & BoundAttributeParameterFlags.IsEnum) != 0;
    public bool IsStringProperty => (_flags & BoundAttributeParameterFlags.IsStringProperty) != 0;
    public bool IsBooleanProperty => (_flags & BoundAttributeParameterFlags.IsBooleanProperty) != 0;

    public MetadataCollection Metadata { get; }

    internal BoundAttributeParameterDescriptor(
        TagHelperKind kind,
        string name,
        string typeName,
        BoundAttributeParameterFlags flags,
        DocumentationObject documentationObject,
        string displayName,
        MetadataCollection metadata,
        ImmutableArray<RazorDiagnostic> diagnostics)
        : base(diagnostics)
    {
        Kind = kind;
        Name = name;
        TypeName = typeName;
        _flags = flags;
        _documentationObject = documentationObject;
        DisplayName = displayName;
        Metadata = metadata ?? MetadataCollection.Empty;
    }

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.AppendData((int)Kind);
        builder.AppendData(Name);
        builder.AppendData(TypeName);
        builder.AppendData((int)Flags);
        builder.AppendData(DisplayName);

        DocumentationObject.AppendToChecksum(in builder);

        builder.AppendData(Metadata.Checksum);
    }

    public string? Documentation => _documentationObject.GetText();

    internal DocumentationObject DocumentationObject => _documentationObject;

    public override string ToString()
        => DisplayName ?? base.ToString()!;
}
