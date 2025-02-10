// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using RazorExtensionsV1_X = Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X.RazorExtensions;
using RazorExtensionsV2_X = Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X.RazorExtensions;
using RazorExtensionsV3 = Microsoft.AspNetCore.Mvc.Razor.Extensions.RazorExtensions;

namespace Microsoft.AspNetCore.Razor.Language;

public static class RazorProjectEngineBuilderExtensions
{
    private static readonly ReadOnlyMemory<char> s_prefix = "MVC-".ToCharArray();

    public static void RegisterExtensions(this RazorProjectEngineBuilder builder)
    {
        var configurationName = builder.Configuration.ConfigurationName.AsSpanOrDefault();

        if (!configurationName.StartsWith(s_prefix.Span))
        {
            return;
        }

        configurationName = configurationName[s_prefix.Length..];

        switch (configurationName)
        {
            case ['1', '.', '0' or '1']: // 1.0 or 1.1
                RazorExtensionsV1_X.Register(builder);

                if (configurationName[^1] == '1') // 1.1.
                {
                    RazorExtensionsV1_X.RegisterViewComponentTagHelpers(builder);
                }

                break;

            case ['2', '.', '0' or '1']: // 2.0 or 2.1
                RazorExtensionsV2_X.Register(builder);
                break;

            case ['3', '.', '0']: // 3.0
                RazorExtensionsV3.Register(builder);
                break;
        }
    }

    /// <summary>
    /// Sets the root namespace for the generated code.
    /// </summary>
    /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
    /// <param name="rootNamespace">The root namespace.</param>
    /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
    public static RazorProjectEngineBuilder SetRootNamespace(this RazorProjectEngineBuilder builder, string? rootNamespace)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Features.Add(new ConfigureRootNamespaceFeature(rootNamespace));
        return builder;
    }

    /// <summary>
    /// Sets the SupportLocalizedComponentNames property to make localized component name diagnostics available.
    /// </summary>
    /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
    /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
    public static RazorProjectEngineBuilder SetSupportLocalizedComponentNames(this RazorProjectEngineBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Features.Add(new ConfigureSupportLocalizedComponentNamesFeature(true));
        return builder;
    }

    /// <summary>
    /// Adds the specified <see cref="ICodeTargetExtension"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
    /// <param name="extension">The <see cref="ICodeTargetExtension"/> to add.</param>
    /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
    public static RazorProjectEngineBuilder AddTargetExtension(this RazorProjectEngineBuilder builder, ICodeTargetExtension extension)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(extension);

        var targetExtensionFeature = GetOrCreateFeature<IRazorTargetExtensionFeature, DefaultRazorTargetExtensionFeature>(builder);
        targetExtensionFeature.TargetExtensions.Add(extension);

        return builder;
    }

    /// <summary>
    /// Adds the specified <see cref="DirectiveDescriptor"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
    /// <param name="directive">The <see cref="DirectiveDescriptor"/> to add.</param>
    /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
    public static RazorProjectEngineBuilder AddDirective(this RazorProjectEngineBuilder builder, DirectiveDescriptor directive)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(directive);

        var directiveFeature = GetOrCreateFeature<ConfigureDirectivesFeature>(builder);
        directiveFeature.AddDirective(directive);

        return builder;
    }

    /// <summary>
    /// Adds the specified <see cref="DirectiveDescriptor"/> for the provided file kind.
    /// </summary>
    /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
    /// <param name="directive">The <see cref="DirectiveDescriptor"/> to add.</param>
    /// <param name="fileKinds">The file kinds, for which to register the directive. See <see cref="FileKinds"/>.</param>
    /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
    public static RazorProjectEngineBuilder AddDirective(this RazorProjectEngineBuilder builder, DirectiveDescriptor directive, params ReadOnlySpan<string> fileKinds)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(directive);

        var feature = GetOrCreateFeature<ConfigureDirectivesFeature>(builder);

        feature.AddDirective(directive, fileKinds);

        return builder;
    }

    private static TFeature GetOrCreateFeature<TFeature>(RazorProjectEngineBuilder builder)
        where TFeature : IRazorFeature, new()
    {
        if (builder.Features.OfType<TFeature>().FirstOrDefault() is not TFeature feature)
        {
            feature = new TFeature();
            builder.Features.Add(feature);
        }

        return feature;
    }

    private static TInterface GetOrCreateFeature<TInterface, TFeature>(RazorProjectEngineBuilder builder)
        where TInterface : IRazorFeature
        where TFeature : TInterface, new()
    {
        if (builder.Features.OfType<TInterface>().FirstOrDefault() is not TInterface feature)
        {
            feature = new TFeature();
            builder.Features.Add(feature);
        }

        return feature;
    }

    private sealed class ConfigureSupportLocalizedComponentNamesFeature(bool value) : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
    {
        public int Order { get; set; }

        public void Configure(RazorCodeGenerationOptionsBuilder options)
        {
            options.SupportLocalizedComponentNames = value;
        }
    }

    private sealed class ConfigureRootNamespaceFeature(string? rootNamespace) : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
    {
        public int Order { get; set; }

        public void Configure(RazorCodeGenerationOptionsBuilder options)
        {
            options.RootNamespace = rootNamespace;
        }
    }
}
