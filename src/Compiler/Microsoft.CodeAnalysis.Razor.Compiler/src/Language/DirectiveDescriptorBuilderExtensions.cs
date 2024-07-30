// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

public static class DirectiveDescriptorBuilderExtensions
{
    public static IDirectiveDescriptorBuilder AddMemberToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(name);
        ArgHelper.ThrowIfNull(description);

        builder.Tokens.Add(
            new(DirectiveTokenKind.Member, Optional: false, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddNamespaceToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(name);
        ArgHelper.ThrowIfNull(description);

        builder.Tokens.Add(
            new(DirectiveTokenKind.Namespace, Optional: false, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddStringToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(name);
        ArgHelper.ThrowIfNull(description);

        builder.Tokens.Add(
            new(DirectiveTokenKind.String, Optional: false, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddTypeToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(name);
        ArgHelper.ThrowIfNull(description);

        builder.Tokens.Add(
            new(DirectiveTokenKind.Type, Optional: false, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddAttributeToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(name);
        ArgHelper.ThrowIfNull(description);

        builder.Tokens.Add(
            new(DirectiveTokenKind.Attribute, Optional: false, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddBooleanToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(name);
        ArgHelper.ThrowIfNull(description);

        builder.Tokens.Add(
            new(DirectiveTokenKind.Boolean, Optional: false, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalMemberToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(name);
        ArgHelper.ThrowIfNull(description);

        builder.Tokens.Add(
            new(DirectiveTokenKind.Member, Optional: true, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalNamespaceToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(name);
        ArgHelper.ThrowIfNull(description);

        builder.Tokens.Add(
            new(DirectiveTokenKind.Namespace, Optional: true, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalStringToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(name);
        ArgHelper.ThrowIfNull(description);

        builder.Tokens.Add(
            new(DirectiveTokenKind.String, Optional: true, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalTypeToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(name);
        ArgHelper.ThrowIfNull(description);

        builder.Tokens.Add(
            new(DirectiveTokenKind.Type, Optional: true, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalAttributeToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(name);
        ArgHelper.ThrowIfNull(description);

        builder.Tokens.Add(
            new(DirectiveTokenKind.Attribute, Optional: true, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddOptionalGenericTypeConstraintToken(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(name);
        ArgHelper.ThrowIfNull(description);

        builder.Tokens.Add(
            new(DirectiveTokenKind.GenericTypeConstraint, Optional: true, name, description));

        return builder;
    }

    public static IDirectiveDescriptorBuilder AddIdentifierOrExpression(this IDirectiveDescriptorBuilder builder, string name, string description)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(name);
        ArgHelper.ThrowIfNull(description);

        builder.Tokens.Add(
            new(DirectiveTokenKind.IdentifierOrExpression, Optional: false, name, description));

        return builder;
    }
}
