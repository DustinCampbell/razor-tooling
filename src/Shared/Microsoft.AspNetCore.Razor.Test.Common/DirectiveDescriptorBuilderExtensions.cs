// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

internal static class DirectiveDescriptorBuilderExtensions
{
    private static DirectiveTokenDescriptor CreateToken(DirectiveTokenKind kind, bool optional)
        => new(kind, optional, Name: null!, Description: null!);

    public static IDirectiveDescriptorBuilder AddMemberToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            CreateToken(DirectiveTokenKind.Member, optional: false));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddNamespaceToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            CreateToken(DirectiveTokenKind.Namespace, optional: false));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddStringToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            CreateToken(DirectiveTokenKind.String, optional: false));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddTypeToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            CreateToken(DirectiveTokenKind.Type, optional: false));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddAttributeToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            CreateToken(DirectiveTokenKind.Attribute, optional: false));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddBooleanToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            CreateToken(DirectiveTokenKind.Boolean, optional: false));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalMemberToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            CreateToken(DirectiveTokenKind.Member, optional: true));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalNamespaceToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            CreateToken(DirectiveTokenKind.Namespace, optional: true));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalStringToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            CreateToken(DirectiveTokenKind.String, optional: true));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalTypeToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            CreateToken(DirectiveTokenKind.Type, optional: true));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalAttributeToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            CreateToken(DirectiveTokenKind.Attribute, optional: true));

        return builder;
    }
}
