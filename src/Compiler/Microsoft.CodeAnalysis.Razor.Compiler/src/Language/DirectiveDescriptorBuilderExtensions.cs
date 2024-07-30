// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

public static class DirectiveDescriptorBuilderExtensions
{
    public static IDirectiveDescriptorBuilder AddMemberToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.Member, optional: false));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddMemberToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.Member, optional: false, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddNamespaceToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.Namespace, optional: false));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddNamespaceToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.Namespace, optional: false, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddStringToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.String, optional: false));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddStringToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.String, optional: false, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddTypeToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.Type, optional: false));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddTypeToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.Type, optional: false, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddAttributeToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.Attribute, optional: false));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddAttributeToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.Attribute, optional: false, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddBooleanToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.Boolean, optional: false));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddBooleanToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.Boolean, optional: false, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalMemberToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.Member, optional: true));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalMemberToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.Member, optional: true, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalNamespaceToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.Namespace, optional: true));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalNamespaceToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.Namespace, optional: true, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalStringToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.String, optional: true));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalStringToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.String, optional: true, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalTypeToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.Type, optional: true));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalTypeToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.Type, optional: true, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalAttributeToken(this IDirectiveDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.Attribute, optional: true));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalAttributeToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.Attribute, optional: true, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalGenericTypeConstraintToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.GenericTypeConstraint, optional: true, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddIdentifierOrExpression(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Tokens.Add(
            DirectiveTokenDescriptor.Create(DirectiveTokenKind.IdentifierOrExpression, optional: false, name, description));

        return builder;
    }
}
