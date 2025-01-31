// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public static class TestTagHelperDescriptorBuilderExtensions
{
    public static TagHelperDescriptorBuilder Metadata(this TagHelperDescriptorBuilder builder, string key, string value)
    {
        ArgHelper.ThrowIfNull(builder);

        return builder.Metadata(new KeyValuePair<string, string>(key, value));
    }

    public static TagHelperDescriptorBuilder Metadata(this TagHelperDescriptorBuilder builder, params KeyValuePair<string, string>[] pairs)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.SetMetadata(pairs);

        return builder;
    }

    public static TagHelperDescriptorBuilder DisplayName(this TagHelperDescriptorBuilder builder, string displayName)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.DisplayName = displayName;

        return builder;
    }

    public static TagHelperDescriptorBuilder AllowChildTag(this TagHelperDescriptorBuilder builder, string allowedChild)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AllowChildTag(childTagBuilder => childTagBuilder.Name = allowedChild);

        return builder;
    }

    public static TagHelperDescriptorBuilder TagOutputHint(this TagHelperDescriptorBuilder builder, string hint)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.TagOutputHint = hint;

        return builder;
    }

    public static TagHelperDescriptorBuilder SetCaseSensitive(this TagHelperDescriptorBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.CaseSensitive = true;

        return builder;
    }

    public static TagHelperDescriptorBuilder ClassifyAttributesOnly(this TagHelperDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.ClassifyAttributesOnly = true;

        return builder;
    }

    public static TagHelperDescriptorBuilder UseFullyQualifiedNameMatch(this TagHelperDescriptorBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.UseFullyQualifiedNameMatch = true;

        return builder;
    }

    public static TagHelperDescriptorBuilder Runtime(this TagHelperDescriptorBuilder builder, string runtime)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Runtime = runtime;

        return builder;
    }

    public static TagHelperDescriptorBuilder Documentation(this TagHelperDescriptorBuilder builder, string documentation)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.SetDocumentation(documentation);

        return builder;
    }

    public static TagHelperDescriptorBuilder AddDiagnostic(this TagHelperDescriptorBuilder builder, RazorDiagnostic diagnostic)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Diagnostics.Add(diagnostic);

        return builder;
    }

    public static TagHelperDescriptorBuilder BoundAttributeDescriptor(
        this TagHelperDescriptorBuilder builder,
        Action<BoundAttributeDescriptorBuilder> configure)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.BindAttribute(configure);

        return builder;
    }

    public static TagHelperDescriptorBuilder TagMatchingRuleDescriptor(
        this TagHelperDescriptorBuilder builder,
        Action<TagMatchingRuleDescriptorBuilder> configure)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.TagMatchingRule(configure);

        return builder;
    }
}
