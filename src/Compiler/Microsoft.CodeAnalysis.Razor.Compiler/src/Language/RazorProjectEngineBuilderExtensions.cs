// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

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
    public static RazorProjectEngineBuilder SetRootNamespace(this RazorProjectEngineBuilder builder, string rootNamespace)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

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
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Features.Add(new SetSupportLocalizedComponentNamesFeature());
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
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (extension == null)
        {
            throw new ArgumentNullException(nameof(extension));
        }

        var targetExtensionFeature = GetTargetExtensionFeature(builder);
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
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (directive == null)
        {
            throw new ArgumentNullException(nameof(directive));
        }

        var directiveFeature = GetDirectiveFeature(builder);
        directiveFeature.Directives.Add(directive);

        return builder;
    }

    /// <summary>
    /// Adds the specified <see cref="DirectiveDescriptor"/> for the provided file kind.
    /// </summary>
    /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
    /// <param name="directive">The <see cref="DirectiveDescriptor"/> to add.</param>
    /// <param name="fileKinds">The file kinds, for which to register the directive. See <see cref="FileKinds"/>.</param>
    /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
    public static RazorProjectEngineBuilder AddDirective(this RazorProjectEngineBuilder builder, DirectiveDescriptor directive, params string[] fileKinds)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (directive == null)
        {
            throw new ArgumentNullException(nameof(directive));
        }

        if (fileKinds == null)
        {
            throw new ArgumentNullException(nameof(fileKinds));
        }

        var directiveFeature = GetDirectiveFeature(builder);

        foreach (var fileKind in fileKinds)
        {
            if (!directiveFeature.DirectivesByFileKind.TryGetValue(fileKind, out var directives))
            {
                directives = new List<DirectiveDescriptor>();
                directiveFeature.DirectivesByFileKind.Add(fileKind, directives);
            }

            directives.Add(directive);
        }

        return builder;
    }

    private static DefaultRazorDirectiveFeature GetDirectiveFeature(RazorProjectEngineBuilder builder)
    {
        var directiveFeature = builder.Features.OfType<DefaultRazorDirectiveFeature>().FirstOrDefault();
        if (directiveFeature == null)
        {
            directiveFeature = new DefaultRazorDirectiveFeature();
            builder.Features.Add(directiveFeature);
        }

        return directiveFeature;
    }

    private static IRazorTargetExtensionFeature GetTargetExtensionFeature(RazorProjectEngineBuilder builder)
    {
        var targetExtensionFeature = builder.Features.OfType<IRazorTargetExtensionFeature>().FirstOrDefault();
        if (targetExtensionFeature == null)
        {
            targetExtensionFeature = new DefaultRazorTargetExtensionFeature();
            builder.Features.Add(targetExtensionFeature);
        }

        return targetExtensionFeature;
    }

    private class SetSupportLocalizedComponentNamesFeature : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
    {
        public int Order { get; set; }

        public void Configure(RazorCodeGenerationOptionsBuilder options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.SupportLocalizedComponentNames = true;
        }
    }

    private class ConfigureRootNamespaceFeature : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
    {
        private readonly string _rootNamespace;

        public ConfigureRootNamespaceFeature(string rootNamespace)
        {
            _rootNamespace = rootNamespace;
        }

        public int Order { get; set; }

        public void Configure(RazorCodeGenerationOptionsBuilder options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.RootNamespace = _rootNamespace;
        }
    }
}
