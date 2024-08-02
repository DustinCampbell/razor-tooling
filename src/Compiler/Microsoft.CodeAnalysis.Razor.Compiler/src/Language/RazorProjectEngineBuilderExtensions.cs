// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        builder.Features.Add(new SetSupportLocalizedComponentNamesFeature());
        return builder;
    }

    public static void SetImportFeature(this RazorProjectEngineBuilder builder, IImportProjectFeature feature)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(feature);

        // Remove any existing import features in favor of the new one we're given.
        var existingFeatures = builder.Features.OfType<IImportProjectFeature>().ToArray();
        foreach (var existingFeature in existingFeatures)
        {
            builder.Features.Remove(existingFeature);
        }

        builder.Features.Add(feature);
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
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(directive);
        ArgHelper.ThrowIfNull(fileKinds);

        var directiveFeature = GetDirectiveFeature(builder);

        foreach (var fileKind in fileKinds)
        {
            if (!directiveFeature.DirectivesByFileKind.TryGetValue(fileKind, out var directives))
            {
                directives = [];
                directiveFeature.DirectivesByFileKind.Add(fileKind, directives);
            }

            directives.Add(directive);
        }

        return builder;
    }

    /// <summary>
    /// Adds the provided <see cref="RazorProjectItem" />s as imports to all project items processed
    /// by the <see cref="RazorProjectEngine"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
    /// <param name="imports">The collection of imports.</param>
    /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
    public static RazorProjectEngineBuilder AddDefaultImports(this RazorProjectEngineBuilder builder, params string[] imports)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Features.Add(new AdditionalImportsProjectFeature(imports));

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

    private sealed class AdditionalImportsProjectFeature(params string[] imports) : RazorEngineFeatureBase, IImportProjectFeature
    {
        private readonly IReadOnlyList<RazorProjectItem> _imports = imports.Select(import => new InMemoryProjectItem(import)).ToArray();

        public IReadOnlyList<RazorProjectItem> GetImports(RazorProjectItem projectItem)
        {
            return _imports;
        }

        private sealed class InMemoryProjectItem : RazorProjectItem
        {
            private readonly byte[] _importBytes;

            public InMemoryProjectItem(string content)
            {
                if (string.IsNullOrEmpty(content))
                {
                    throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(content));
                }

                var preamble = Encoding.UTF8.GetPreamble();
                var contentBytes = Encoding.UTF8.GetBytes(content);

                _importBytes = new byte[preamble.Length + contentBytes.Length];
                preamble.CopyTo(_importBytes, 0);
                contentBytes.CopyTo(_importBytes, preamble.Length);
            }

            public override string BasePath => null!;

            public override string FilePath => null!;

            public override string PhysicalPath => null!;

            public override bool Exists => true;

            public override Stream Read() => new MemoryStream(_importBytes);
        }
    }

    private sealed class SetSupportLocalizedComponentNamesFeature : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
    {
        public int Order { get; set; }

        public void Configure(RazorCodeGenerationOptionsBuilder builder)
        {
            ArgHelper.ThrowIfNull(builder);

            builder.SupportLocalizedComponentNames = true;
        }
    }

    private sealed class ConfigureRootNamespaceFeature(string? rootNamespace) : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
    {
        private readonly string? _rootNamespace = rootNamespace;

        public int Order { get; set; }

        public void Configure(RazorCodeGenerationOptionsBuilder builder)
        {
            ArgHelper.ThrowIfNull(builder);

            builder.RootNamespace = _rootNamespace;
        }
    }
}
