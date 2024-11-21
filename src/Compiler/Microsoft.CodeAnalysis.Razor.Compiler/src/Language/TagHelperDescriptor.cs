// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public sealed class TagHelperDescriptor : TagHelperObject<TagHelperDescriptor>
{
    private readonly TagHelperFlags _flags;
    private readonly DocumentationObject _documentationObject;

    private ImmutableArray<BoundAttributeDescriptor> _editorRequiredAttributes;

    public TagHelperKind Kind { get; }
    public RuntimeKind RuntimeKind { get; }

    public string Name { get; }
    public string AssemblyName { get; }

    public string? Documentation => _documentationObject.GetText();
    internal DocumentationObject DocumentationObject => _documentationObject;

    public string? TypeName { get; }
    public string? TypeNamespace { get; }
    public string? TypeNameIdentifier { get; }

    public string DisplayName { get; }
    public string? TagOutputHint { get; }

    public bool CaseSensitive => (_flags & TagHelperFlags.CaseSensitive) != 0;

    public ImmutableArray<AllowedChildTagDescriptor> AllowedChildTags { get; }
    public ImmutableArray<BoundAttributeDescriptor> BoundAttributes { get; }
    public ImmutableArray<TagMatchingRuleDescriptor> TagMatchingRules { get; }

    public MetadataCollection Metadata { get; }

    internal TagHelperFlags Flags => _flags;

    internal bool IsComponentTagHelper => Kind == TagHelperKind.Component;
    internal bool IsChildContentTagHelper => Kind == TagHelperKind.ChildContent;
    internal bool IsComponentOrChildContentTagHelper => IsComponentTagHelper || IsChildContentTagHelper;

    /// <summary>
    /// Gets whether the component matches a tag with a fully qualified name.
    /// </summary>
    internal bool IsComponentFullyQualifiedNameMatch => (_flags & TagHelperFlags.IsComponentFullyQualifiedNameMatch) != 0;

    internal bool ClassifyAttributesOnly => (_flags & TagHelperFlags.ClassifyAttributesOnly) != 0;

    internal TagHelperDescriptor(
        TagHelperKind kind,
        RuntimeKind runtimeKind,
        string name,
        string assemblyName,
        string? typeName,
        string? typeNamespace,
        string? typeNameIdentifier,
        string displayName,
        TagHelperFlags flags,
        DocumentationObject documentationObject,
        string? tagOutputHint,
        ImmutableArray<TagMatchingRuleDescriptor> tagMatchingRules,
        ImmutableArray<BoundAttributeDescriptor> attributeDescriptors,
        ImmutableArray<AllowedChildTagDescriptor> allowedChildTags,
        MetadataCollection metadata,
        ImmutableArray<RazorDiagnostic> diagnostics)
        : base(diagnostics)
    {
        Kind = kind;
        RuntimeKind = runtimeKind;
        Name = name;
        AssemblyName = assemblyName;
        TypeName = typeName;
        TypeNamespace = typeNamespace;
        TypeNameIdentifier = typeNameIdentifier;
        DisplayName = displayName;
        _flags = flags;
        _documentationObject = documentationObject;
        TagOutputHint = tagOutputHint;
        TagMatchingRules = tagMatchingRules.NullToEmpty();
        BoundAttributes = attributeDescriptors.NullToEmpty();
        AllowedChildTags = allowedChildTags.NullToEmpty();
        Metadata = metadata ?? MetadataCollection.Empty;
    }

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.AppendData((int)Kind);
        builder.AppendData((int)RuntimeKind);
        builder.AppendData(Name);
        builder.AppendData(AssemblyName);
        builder.AppendData(TypeName);
        builder.AppendData(TypeNamespace);
        builder.AppendData(TypeNameIdentifier);
        builder.AppendData(DisplayName);
        builder.AppendData(TagOutputHint);

        DocumentationObject.AppendToChecksum(in builder);

        builder.AppendData((int)Flags);

        foreach (var descriptor in AllowedChildTags)
        {
            builder.AppendData(descriptor.Checksum);
        }

        foreach (var descriptor in BoundAttributes)
        {
            builder.AppendData(descriptor.Checksum);
        }

        foreach (var descriptor in TagMatchingRules)
        {
            builder.AppendData(descriptor.Checksum);
        }

        builder.AppendData(Metadata.Checksum);
    }

    internal ImmutableArray<BoundAttributeDescriptor> EditorRequiredAttributes
    {
        get
        {
            if (_editorRequiredAttributes.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref _editorRequiredAttributes, GetEditorRequiredAttributes(BoundAttributes));
            }

            return _editorRequiredAttributes;

            static ImmutableArray<BoundAttributeDescriptor> GetEditorRequiredAttributes(ImmutableArray<BoundAttributeDescriptor> attributes)
            {
                if (attributes.Length == 0)
                {
                    return ImmutableArray<BoundAttributeDescriptor>.Empty;
                }

                using var results = new PooledArrayBuilder<BoundAttributeDescriptor>(capacity: attributes.Length);

                foreach (var attribute in attributes)
                {
                    if (attribute is { IsEditorRequired: true } editorRequiredAttribute)
                    {
                        results.Add(editorRequiredAttribute);
                    }
                }

                return results.DrainToImmutable();
            }
        }
    }

    public IEnumerable<RazorDiagnostic> GetAllDiagnostics()
    {
        foreach (var allowedChildTag in AllowedChildTags)
        {
            foreach (var diagnostic in allowedChildTag.Diagnostics)
            {
                yield return diagnostic;
            }
        }

        foreach (var boundAttribute in BoundAttributes)
        {
            foreach (var diagnostic in boundAttribute.Diagnostics)
            {
                yield return diagnostic;
            }
        }

        foreach (var tagMatchingRule in TagMatchingRules)
        {
            foreach (var diagnostic in tagMatchingRule.Diagnostics)
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

    private string GetDebuggerDisplay()
    {
        return $"{DisplayName} - {string.Join(" | ", TagMatchingRules.Select(r => r.GetDebuggerDisplay()))}";
    }
}
