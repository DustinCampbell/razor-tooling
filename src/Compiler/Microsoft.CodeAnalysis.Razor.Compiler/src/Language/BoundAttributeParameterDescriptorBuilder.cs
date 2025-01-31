// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class BoundAttributeParameterDescriptorBuilder : TagHelperObjectBuilder<BoundAttributeParameterDescriptor>
{
    [AllowNull]
    private BoundAttributeDescriptorBuilder _parent;
    private TagHelperKind _kind;
    private DocumentationObject _documentationObject;
    private MetadataCollection? _metadata;

    private BoundAttributeParameterDescriptorBuilder()
    {
    }

    internal BoundAttributeParameterDescriptorBuilder(BoundAttributeDescriptorBuilder parent, TagHelperKind kind)
    {
        _parent = parent;
        _kind = kind;
    }

    public string? Name { get; set; }
    public string? TypeName { get; set; }
    public bool IsEnum { get; set; }

    public string? Documentation
    {
        get => _documentationObject.GetText();
        set => _documentationObject = new(value);
    }

    public string? DisplayName { get; set; }

    public void SetMetadata(MetadataCollection metadata)
    {
        _metadata = metadata ?? MetadataCollection.Empty;
    }

    public bool TryGetMetadataValue(string key, [NotNullWhen(true)] out string? value)
    {
        if (_metadata is { } metadata)
        {
            return metadata.TryGetValue(key, out value);
        }

        value = null;
        return false;
    }

    internal bool CaseSensitive => _parent.CaseSensitive;

    internal void SetDocumentation(string? text)
    {
        _documentationObject = new(text);
    }

    internal void SetDocumentation(DocumentationDescriptor? documentation)
    {
        _documentationObject = new(documentation);
    }

    private protected override BoundAttributeParameterDescriptor BuildCore(ImmutableArray<RazorDiagnostic> diagnostics)
    {
        var flags = ComputeFlags(IsEnum, CaseSensitive, TypeName);

        return new BoundAttributeParameterDescriptor(
            _kind,
            Name ?? string.Empty,
            TypeName ?? string.Empty,
            flags,
            _documentationObject,
            GetDisplayName(),
            _metadata ?? MetadataCollection.Empty,
            diagnostics);
    }

    internal static BoundAttributeParameterFlags ComputeFlags(bool isEnum, bool caseSensitive, string? typeName)
    {
        BoundAttributeParameterFlags flags = 0;

        if (isEnum)
        {
            flags |= BoundAttributeParameterFlags.IsEnum;
        }

        if (caseSensitive)
        {
            flags |= BoundAttributeParameterFlags.CaseSensitive;
        }

        if (typeName == typeof(string).FullName || typeName == "string")
        {
            flags |= BoundAttributeParameterFlags.IsStringProperty;
        }

        if (typeName == typeof(bool).FullName || typeName == "bool")
        {
            flags |= BoundAttributeParameterFlags.IsBooleanProperty;
        }

        return flags;
    }

    private string GetDisplayName()
        => DisplayName ?? $":{Name}";

    private protected override void CollectDiagnostics(ref PooledHashSet<RazorDiagnostic> diagnostics)
    {
        if (Name.IsNullOrWhiteSpace())
        {
            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeParameterNullOrWhitespace(_parent.Name);

            diagnostics.Add(diagnostic);
        }
        else
        {
            foreach (var character in Name)
            {
                if (char.IsWhiteSpace(character) || HtmlConventions.IsInvalidNonWhitespaceHtmlCharacters(character))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeParameterName(
                        _parent.Name,
                        Name,
                        character);

                    diagnostics.Add(diagnostic);
                }
            }
        }
    }
}
