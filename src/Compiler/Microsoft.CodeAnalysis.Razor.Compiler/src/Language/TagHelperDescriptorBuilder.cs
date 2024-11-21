// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class TagHelperDescriptorBuilder : TagHelperObjectBuilder<TagHelperDescriptor>
{
    private TagHelperKind _kind;
    private RuntimeKind _runtimeKind;
    private string? _name;
    private string? _assemblyName;
    private TagHelperFlags _flags;

    private DocumentationObject _documentationObject;
    private MetadataHolder _metadata;

    private TagHelperDescriptorBuilder()
    {
    }

    internal TagHelperDescriptorBuilder(TagHelperKind kind, string name, string assemblyName)
        : this(kind, RuntimeKind.Default, name, assemblyName)
    {
    }

    internal TagHelperDescriptorBuilder(TagHelperKind kind, RuntimeKind runtimeKind, string name, string assemblyName)
        : this()
    {
        _kind = kind;
        _runtimeKind = runtimeKind;
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _assemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
    }

    public static TagHelperDescriptorBuilder Create(string name, string assemblyName)
        => new(TagHelperKind.Default, name, assemblyName);

    public static TagHelperDescriptorBuilder Create(TagHelperKind kind, string name, string assemblyName)
        => new(kind, name, assemblyName);

    public static TagHelperDescriptorBuilder Create(TagHelperKind kind, RuntimeKind runtimeKind, string name, string assemblyName)
        => new(kind, runtimeKind, name, assemblyName);

    public TagHelperKind Kind => _kind;
    public RuntimeKind RuntimeKind => _runtimeKind;
    public string Name => _name.AssumeNotNull();
    public string AssemblyName => _assemblyName.AssumeNotNull();
    public string? TypeName { get; set; }
    public string? TypeNamespace { get; set; }
    public string? TypeNameIdentifier { get; set; }
    public string? DisplayName { get; set; }
    public string? TagOutputHint { get; set; }

    public bool CaseSensitive
    {
        get => _flags.IsFlagSet(TagHelperFlags.CaseSensitive);
        set => _flags.UpdateFlag(TagHelperFlags.CaseSensitive, value);
    }

    internal bool IsComponentFullyQualifiedNameMatch
    {
        get => _flags.IsFlagSet(TagHelperFlags.IsComponentFullyQualifiedNameMatch);
        set => _flags.UpdateFlag(TagHelperFlags.IsComponentFullyQualifiedNameMatch, value);
    }

    internal bool ClassifyAttributesOnly
    {
        get => _flags.IsFlagSet(TagHelperFlags.ClassifyAttributesOnly);
        set => _flags.UpdateFlag(TagHelperFlags.ClassifyAttributesOnly, value);
    }

    internal bool IsBindFallback
    {
        get => _flags.IsFlagSet(TagHelperFlags.IsBindFallback);
        set
        {
            if (Kind is not TagHelperKind.Bind)
            {
                ThrowHelper.ThrowInvalidOperationException($"Invalid flag for {Kind} tag helper");
            }

            _flags.UpdateFlag(TagHelperFlags.IsBindFallback, value);
        }
    }

    internal TagHelperFlags Flags
    {
        get => _flags;
        set => _flags = value;
    }

    public string? Documentation
    {
        get => _documentationObject.GetText();
        set => _documentationObject = new(value);
    }

    public IDictionary<string, string?> Metadata => _metadata.MetadataDictionary;

    public void SetMetadata(MetadataCollection metadata) => _metadata.SetMetadataCollection(metadata);

    public bool TryGetMetadataValue(string key, [NotNullWhen(true)] out string? value)
        => _metadata.TryGetMetadataValue(key, out value);

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
        var metadata = _metadata.GetMetadataCollection();

        return new TagHelperDescriptor(
            Kind,
            RuntimeKind,
            Name,
            AssemblyName,
            TypeName,
            TypeNamespace,
            TypeNameIdentifier,
            GetDisplayName(),
            _flags,
            _documentationObject,
            TagOutputHint,
            TagMatchingRules.ToImmutable(),
            BoundAttributes.ToImmutable(),
            AllowedChildTags.ToImmutable(),
            metadata,
            diagnostics);
    }

    internal string GetDisplayName()
        => DisplayName ?? TypeName ?? Name;
}
