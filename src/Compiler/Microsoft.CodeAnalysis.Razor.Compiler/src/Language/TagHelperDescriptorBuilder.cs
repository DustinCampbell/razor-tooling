// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class TagHelperDescriptorBuilder : TagHelperObjectBuilder<TagHelperDescriptor>
{
    private TagHelperKind _kind;
    private string? _name;
    private string? _assemblyName;

    private DocumentationObject _documentationObject;
    private MetadataCollection? _metadata;

    private TagHelperDescriptorBuilder()
    {
    }

    internal TagHelperDescriptorBuilder(TagHelperKind kind, string name, string assemblyName)
        : this()
    {
        _kind = kind;
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _assemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));

        Runtime = RuntimeKind.Default;
    }

    public static TagHelperDescriptorBuilder Create(string name, string assemblyName)
        => new(TagHelperKind.Default, name, assemblyName);

    public static TagHelperDescriptorBuilder Create(TagHelperKind kind, string name, string assemblyName)
        => new(kind, name, assemblyName);

    public TagHelperKind Kind => _kind;
    public string Name => _name.AssumeNotNull();
    public string AssemblyName => _assemblyName.AssumeNotNull();
    public RuntimeKind Runtime { get; set; }
    public string? DisplayName { get; set; }
    public string? TagOutputHint { get; set; }
    public bool CaseSensitive { get; set; }
    public bool ClassifyAttributesOnly { get; set; }
    public bool UseFullyQualifiedNameMatch { get; set; }

    public string? Documentation
    {
        get => _documentationObject.GetText();
        set => _documentationObject = new(value);
    }

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

    public TagHelperObjectBuilderCollection<AllowedChildTagDescriptor, AllowedChildTagDescriptorBuilder> AllowedChildTags { get; }
        = new(AllowedChildTagDescriptorBuilder.Pool);

    public TagHelperObjectBuilderCollection<BoundAttributeDescriptor, BoundAttributeDescriptorBuilder> BoundAttributes { get; }
        = new(BoundAttributeDescriptorBuilder.Pool);

    public TagHelperObjectBuilderCollection<TagMatchingRuleDescriptor, TagMatchingRuleDescriptorBuilder> TagMatchingRules { get; }
        = new(TagMatchingRuleDescriptorBuilder.Pool);

    public void AllowChildTag(Action<AllowedChildTagDescriptorBuilder> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = AllowedChildTagDescriptorBuilder.GetInstance(this);
        configure(builder);
        AllowedChildTags.Add(builder);
    }

    public void BindAttribute(Action<BoundAttributeDescriptorBuilder> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = BoundAttributeDescriptorBuilder.GetInstance(this, Kind);
        configure(builder);
        BoundAttributes.Add(builder);
    }

    public void TagMatchingRule(Action<TagMatchingRuleDescriptorBuilder> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = TagMatchingRuleDescriptorBuilder.GetInstance(this);
        configure(builder);
        TagMatchingRules.Add(builder);
    }

    internal void SetDocumentation(string? text)
    {
        _documentationObject = new(text);
    }

    internal void SetDocumentation(DocumentationDescriptor? documentation)
    {
        _documentationObject = new(documentation);
    }

    private protected override TagHelperDescriptor BuildCore(ImmutableArray<RazorDiagnostic> diagnostics)
    {
        return new TagHelperDescriptor(
            Kind,
            Name,
            AssemblyName,
            ComputeFlags(),
            Runtime,
            GetDisplayName(),
            _documentationObject,
            TagOutputHint,
            TagMatchingRules.ToImmutable(),
            BoundAttributes.ToImmutable(),
            AllowedChildTags.ToImmutable(),
            _metadata ?? MetadataCollection.Empty,
            diagnostics);
    }

    private TagHelperFlags ComputeFlags()
    {
        TagHelperFlags flags = 0;

        if (CaseSensitive)
        {
            flags |= TagHelperFlags.CaseSensitive;
        }

        if (ClassifyAttributesOnly)
        {
            flags |= TagHelperFlags.ClassifyAttributesOnly;
        }

        if (Kind == TagHelperKind.ChildContent)
        {
            flags |= TagHelperFlags.IsChildContent;
        }

        if (UseFullyQualifiedNameMatch)
        {
            flags |= TagHelperFlags.UseFullyQualifiedNameMatch;
        }

        return flags;
    }

    internal string GetDisplayName()
    {
        return DisplayName ?? GetTypeName() ?? Name;

        string? GetTypeName()
        {
            return TryGetMetadataValue(TagHelperMetadata.Common.TypeName, out var value)
                ? value
                : null;
        }
    }
}
