// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

public sealed record class DirectiveTokenDescriptor
{
    public DirectiveTokenKind Kind { get; }
    public bool Optional { get; }
    public string? Name { get; }
    public string? Description { get; }

    private DirectiveTokenDescriptor(DirectiveTokenKind kind, bool optional, string? name, string? description)
    {
        Kind = kind;
        Optional = optional;
        Name = name;
        Description = description;
    }

    public static DirectiveTokenDescriptor Create(DirectiveTokenKind kind)
        => new(kind, optional: false, name: null, description: null);

    public static DirectiveTokenDescriptor Create(DirectiveTokenKind kind, bool optional)
        => new(kind, optional, name: null, description: null);

    public static DirectiveTokenDescriptor Create(DirectiveTokenKind kind, bool optional, string name, string description)
        => new(kind, optional, name, description);
}
