// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public class RazorProjectEngine
{
    public RazorConfiguration Configuration { get; }
    public RazorProjectFileSystem FileSystem { get; }
    public ImmutableArray<IRazorEngineFeature> Features { get; }
    public ImmutableArray<IRazorEnginePhase> Phases { get; }

    internal RazorProjectEngine(
        RazorConfiguration configuration,
        RazorProjectFileSystem fileSystem,
        ImmutableArray<IRazorEngineFeature> features,
        ImmutableArray<IRazorEnginePhase> phases)
    {
        ArgHelper.ThrowIfNull(configuration);
        ArgHelper.ThrowIfNull(fileSystem);

        Configuration = configuration;
        FileSystem = fileSystem;
        Features = features;
        Phases = phases;

        foreach (var feature in features)
        {
            feature.Initialize(this);
        }

        foreach (var phase in phases)
        {
            phase.Initialize(this);
        }
    }

    private protected RazorProjectEngine(RazorProjectEngine otherProjectEngine)
    {
        ArgHelper.ThrowIfNull(otherProjectEngine);

        Configuration = otherProjectEngine.Configuration;
        FileSystem = otherProjectEngine.FileSystem;
        Features = otherProjectEngine.Features;
        Phases = otherProjectEngine.Phases;
    }

    public RazorCodeDocument Process(RazorProjectItem projectItem)
    {
        var codeDocument = CreateCodeDocumentCore(projectItem);
        RunPhasesOn(codeDocument);
        return codeDocument;
    }

    public RazorCodeDocument Process(
        RazorSourceDocument sourceDocument,
        string? fileKind,
        ImmutableArray<RazorSourceDocument> importSources,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
    {
        var codeDocument = CreateCodeDocumentCore(sourceDocument, fileKind, importSources, tagHelpers);
        RunPhasesOn(codeDocument);
        return codeDocument;
    }

    public RazorCodeDocument ProcessDeclarationOnly(RazorProjectItem projectItem)
    {
        var codeDocument = CreateCodeDocumentCore(projectItem,
            configureCodeGenerationOptions: builder =>
            {
                builder.SuppressPrimaryMethodBody = true;
            });

        RunPhasesOn(codeDocument);
        return codeDocument;
    }

    public RazorCodeDocument ProcessDeclarationOnly(
        RazorSourceDocument sourceDocument,
        string fileKind,
        ImmutableArray<RazorSourceDocument> importSources,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
    {
        var codeDocument = CreateCodeDocumentCore(sourceDocument, fileKind, importSources, tagHelpers,
            configureCodeGenerationOptions: builder =>
            {
                builder.SuppressPrimaryMethodBody = true;
            });

        RunPhasesOn(codeDocument);
        return codeDocument;
    }

    public RazorCodeDocument ProcessDesignTime(RazorProjectItem projectItem)
    {
        var codeDocument = CreateCodeDocumentDesignTimeCore(projectItem);
        RunPhasesOn(codeDocument);
        return codeDocument;
    }

    public RazorCodeDocument ProcessDesignTime(
        RazorSourceDocument sourceDocument,
        string? fileKind,
        ImmutableArray<RazorSourceDocument> importSources,
        IReadOnlyList<TagHelperDescriptor>? tagHelpers)
    {
        var codeDocument = CreateCodeDocumentDesignTimeCore(sourceDocument, fileKind, importSources, tagHelpers);
        RunPhasesOn(codeDocument);
        return codeDocument;
    }

    private protected RazorCodeDocument CreateCodeDocumentCore(
        RazorProjectItem projectItem,
        Action<RazorParserOptionsBuilder>? configureParserOptions = null,
        Action<RazorCodeGenerationOptionsBuilder>? configureCodeGenerationOptions = null)
    {
        ArgHelper.ThrowIfNull(projectItem);

        var sourceDocument = RazorSourceDocument.ReadFrom(projectItem);

        using var importItems = new PooledArrayBuilder<RazorProjectItem>();

        foreach (var feature in Features)
        {
            if (feature is IImportProjectFeature importProjectFeature)
            {
                importItems.AddRange(importProjectFeature.GetImports(projectItem));
            }
        }

        var importSourceDocuments = GetImportSourceDocuments(importItems.DrainToImmutable());
        return CreateCodeDocumentCore(
            sourceDocument,
            projectItem.FileKind,
            importSourceDocuments,
            tagHelpers: null,
            configureParserOptions,
            configureCodeGenerationOptions,
            cssScope: projectItem.CssScope);
    }

    internal RazorCodeDocument CreateCodeDocumentCore(
        RazorSourceDocument sourceDocument,
        string? fileKind = null,
        ImmutableArray<RazorSourceDocument> importSourceDocuments = default,
        IReadOnlyList<TagHelperDescriptor>? tagHelpers = null,
        Action<RazorParserOptionsBuilder>? configureParserOptions = null,
        Action<RazorCodeGenerationOptionsBuilder>? configureCodeGenerationOptions = null,
        string? cssScope = null)
    {
        ArgHelper.ThrowIfNull(sourceDocument);

        var parserOptions = GetRequiredFeature<IRazorParserOptionsFactory>().Create(fileKind, builder =>
        {
            configureParserOptions?.Invoke(builder);
        });

        var codeGenerationOptions = GetRequiredFeature<IRazorCodeGenerationOptionsFactory>().Create(fileKind, builder =>
        {
            configureCodeGenerationOptions?.Invoke(builder);
        });

        var codeDocument = RazorCodeDocument.Create(sourceDocument, importSourceDocuments, parserOptions, codeGenerationOptions);
        codeDocument.SetTagHelpers(tagHelpers);

        if (fileKind != null)
        {
            codeDocument.SetFileKind(fileKind);
        }

        if (cssScope != null)
        {
            codeDocument.SetCssScope(cssScope);
        }

        return codeDocument;
    }

    private protected RazorCodeDocument CreateCodeDocumentDesignTimeCore(
        RazorProjectItem projectItem,
        Action<RazorParserOptionsBuilder>? configureParserOptions = null,
        Action<RazorCodeGenerationOptionsBuilder>? configureCodeGenerationOptions = null)
    {
        ArgHelper.ThrowIfNull(projectItem);

        var sourceDocument = RazorSourceDocument.ReadFrom(projectItem);

        using var importItems = new PooledArrayBuilder<RazorProjectItem>();

        foreach (var feature in Features)
        {
            if (feature is IImportProjectFeature importProjectFeature)
            {
                importItems.AddRange(importProjectFeature.GetImports(projectItem));
            }
        }

        var importSourceDocuments = GetImportSourceDocuments(importItems.DrainToImmutable(), suppressExceptions: true);

        return CreateCodeDocumentDesignTimeCore(
            sourceDocument,
            projectItem.FileKind,
            importSourceDocuments,
            tagHelpers: null,
            configureParserOptions,
            configureCodeGenerationOptions);
    }

    private RazorCodeDocument CreateCodeDocumentDesignTimeCore(
        RazorSourceDocument sourceDocument,
        string? fileKind,
        ImmutableArray<RazorSourceDocument> importSourceDocuments,
        IReadOnlyList<TagHelperDescriptor>? tagHelpers,
        Action<RazorParserOptionsBuilder>? configureParserOptions = null,
        Action<RazorCodeGenerationOptionsBuilder>? configureCodeGenerationOptions = null)
    {
        ArgHelper.ThrowIfNull(sourceDocument);

        var parserOptions = GetRequiredFeature<IRazorParserOptionsFactory>().Create(fileKind, builder =>
        {
            ConfigureDesignTimeParserOptions(builder);
            configureParserOptions?.Invoke(builder);
        });

        var codeGenerationOptions = GetRequiredFeature<IRazorCodeGenerationOptionsFactory>().Create(fileKind, builder =>
        {
            ConfigureDesignTimeCodeGenerationOptions(builder);
            configureCodeGenerationOptions?.Invoke(builder);
        });

        var codeDocument = RazorCodeDocument.Create(sourceDocument, importSourceDocuments, parserOptions, codeGenerationOptions);
        codeDocument.SetTagHelpers(tagHelpers);

        if (fileKind != null)
        {
            codeDocument.SetFileKind(fileKind);
        }

        return codeDocument;
    }

    public void RunPhasesOn(RazorCodeDocument codeDocument)
    {
        ArgHelper.ThrowIfNull(codeDocument);

        foreach (var phase in Phases)
        {
            phase.Execute(codeDocument);
        }
    }

    internal bool TryGetFeature<TFeature>([NotNullWhen(true)] out TFeature? feature)
        where TFeature : class, IRazorEngineFeature
    {
        foreach (var item in Features)
        {
            if (item is TFeature result)
            {
                feature = result;
                return true;
            }
        }

        feature = null;
        return false;
    }

    private TFeature GetRequiredFeature<TFeature>()
        where TFeature : IRazorEngineFeature
    {
        foreach (var feature in Features)
        {
            if (feature is TFeature result)
            {
                return result;
            }
        }

        throw new InvalidOperationException(
            Resources.FormatRazorProjectEngineMissingFeatureDependency(
                typeof(RazorProjectEngine).FullName,
                typeof(TFeature).FullName));
    }

    internal static RazorProjectEngine CreateEmpty(Action<RazorProjectEngineBuilder>? configure = null)
    {
        var builder = new RazorProjectEngineBuilder(RazorConfiguration.Default, RazorProjectFileSystem.Empty);

        configure?.Invoke(builder);

        return builder.Build();
    }

    internal static RazorProjectEngine Create(Action<RazorProjectEngineBuilder> configure)
        => Create(RazorConfiguration.Default, RazorProjectFileSystem.Empty, configure);

    public static RazorProjectEngine Create(RazorConfiguration configuration, RazorProjectFileSystem fileSystem)
        => Create(configuration, fileSystem, configure: null);

    public static RazorProjectEngine Create(
        RazorConfiguration configuration,
        RazorProjectFileSystem fileSystem,
        Action<RazorProjectEngineBuilder>? configure)
    {
        ArgHelper.ThrowIfNull(configuration);
        ArgHelper.ThrowIfNull(fileSystem);

        var builder = new RazorProjectEngineBuilder(configuration, fileSystem);

        // The initialization order is somewhat important.
        //
        // Defaults -> Extensions -> Additional customization
        //
        // This allows extensions to rely on default features, and customizations to override choices made by
        // extensions.
        AddDefaultPhases(builder.Phases);
        AddDefaultFeatures(builder.Features);

        if (configuration.LanguageVersion >= RazorLanguageVersion.Version_5_0)
        {
            builder.Features.Add(new ViewCssScopePass());
        }

        if (configuration.LanguageVersion >= RazorLanguageVersion.Version_3_0)
        {
            FunctionsDirective.Register(builder);
            ImplementsDirective.Register(builder);
            InheritsDirective.Register(builder);
            NamespaceDirective.Register(builder);
            AttributeDirective.Register(builder);

            AddComponentFeatures(builder, configuration.LanguageVersion);
        }

        configure?.Invoke(builder);

        return builder.Build();
    }

    private static void AddDefaultPhases(ImmutableArray<IRazorEnginePhase>.Builder phases)
    {
        phases.Add(new DefaultRazorParsingPhase());
        phases.Add(new DefaultRazorSyntaxTreePhase());
        phases.Add(new DefaultRazorTagHelperContextDiscoveryPhase());
        phases.Add(new DefaultRazorTagHelperRewritePhase());
        phases.Add(new DefaultRazorIntermediateNodeLoweringPhase());
        phases.Add(new DefaultRazorDocumentClassifierPhase());
        phases.Add(new DefaultRazorDirectiveClassifierPhase());
        phases.Add(new DefaultRazorOptimizationPhase());
        phases.Add(new DefaultRazorCSharpLoweringPhase());
    }

    private static void AddDefaultFeatures(ImmutableArray<IRazorEngineFeature>.Builder features)
    {
        features.Add(new DefaultImportProjectFeature());

        // General extensibility
        features.Add(new DefaultRazorDirectiveFeature());
        features.Add(new DefaultMetadataIdentifierFeature());

        // Options features
        features.Add(new RazorParserOptionsFactory());
        features.Add(new RazorCodeGenerationOptionsFactory());

        // Syntax Tree passes
        features.Add(new DefaultDirectiveSyntaxTreePass());
        features.Add(new HtmlNodeOptimizationPass());

        // Intermediate Node Passes
        features.Add(new DefaultDocumentClassifierPass());
        features.Add(new MetadataAttributePass());
        features.Add(new DesignTimeDirectivePass());
        features.Add(new DirectiveRemovalOptimizationPass());
        features.Add(new DefaultTagHelperOptimizationPass());
        features.Add(new PreallocatedTagHelperAttributeOptimizationPass());
        features.Add(new EliminateMethodBodyPass());

        // Default Code Target Extensions
        var targetExtensionFeature = new DefaultRazorTargetExtensionFeature();
        features.Add(targetExtensionFeature);
        targetExtensionFeature.TargetExtensions.Add(new MetadataAttributeTargetExtension());
        targetExtensionFeature.TargetExtensions.Add(new DefaultTagHelperTargetExtension());
        targetExtensionFeature.TargetExtensions.Add(new PreallocatedAttributeTargetExtension());
        targetExtensionFeature.TargetExtensions.Add(new DesignTimeDirectiveTargetExtension());

        // Default configuration
        var configurationFeature = new DefaultDocumentClassifierPassFeature();
        features.Add(configurationFeature);
        configurationFeature.ConfigureClass.Add((document, @class) =>
        {
            @class.ClassName = "Template";
            @class.Modifiers.Add("public");
        });

        configurationFeature.ConfigureNamespace.Add((document, @namespace) =>
        {
            @namespace.Content = "Razor";
        });

        configurationFeature.ConfigureMethod.Add((document, method) =>
        {
            method.MethodName = "ExecuteAsync";
            method.ReturnType = $"global::{typeof(Task).FullName}";

            method.Modifiers.Add("public");
            method.Modifiers.Add("async");
            method.Modifiers.Add("override");
        });
    }

    private static void AddComponentFeatures(RazorProjectEngineBuilder builder, RazorLanguageVersion razorLanguageVersion)
    {
        // Project Engine Features
        builder.Features.Add(new ComponentImportProjectFeature());

        // Directives (conditional on file kind)
        ComponentCodeDirective.Register(builder);
        ComponentInjectDirective.Register(builder);
        ComponentLayoutDirective.Register(builder);
        ComponentPageDirective.Register(builder);

        if (razorLanguageVersion >= RazorLanguageVersion.Version_6_0)
        {
            ComponentConstrainedTypeParamDirective.Register(builder);
        }
        else
        {
            ComponentTypeParamDirective.Register(builder);
        }

        if (razorLanguageVersion >= RazorLanguageVersion.Version_5_0)
        {
            ComponentPreserveWhitespaceDirective.Register(builder);
        }

        if (razorLanguageVersion >= RazorLanguageVersion.Version_8_0)
        {
            ComponentRenderModeDirective.Register(builder);
        }

        // Document Classifier
        builder.Features.Add(new ComponentDocumentClassifierPass(razorLanguageVersion));

        // Directive Classifier
        builder.Features.Add(new ComponentWhitespacePass());

        // Optimization
        builder.Features.Add(new ComponentComplexAttributeContentPass());
        builder.Features.Add(new ComponentLoweringPass());
        builder.Features.Add(new ComponentEventHandlerLoweringPass());
        builder.Features.Add(new ComponentKeyLoweringPass());
        builder.Features.Add(new ComponentReferenceCaptureLoweringPass());
        builder.Features.Add(new ComponentSplatLoweringPass());
        builder.Features.Add(new ComponentFormNameLoweringPass());
        builder.Features.Add(new ComponentBindLoweringPass(razorLanguageVersion >= RazorLanguageVersion.Version_7_0));
        builder.Features.Add(new ComponentRenderModeLoweringPass());
        builder.Features.Add(new ComponentCssScopePass());
        builder.Features.Add(new ComponentTemplateDiagnosticPass());
        builder.Features.Add(new ComponentGenericTypePass());
        builder.Features.Add(new ComponentChildContentDiagnosticPass());
        builder.Features.Add(new ComponentMarkupDiagnosticPass());
        builder.Features.Add(new ComponentMarkupBlockPass(razorLanguageVersion));
        builder.Features.Add(new ComponentMarkupEncodingPass(razorLanguageVersion));
    }

    // Internal for testing
    internal static ImmutableArray<RazorSourceDocument> GetImportSourceDocuments(
        ImmutableArray<RazorProjectItem> importItems,
        bool suppressExceptions = false)
    {
        using var imports = new PooledArrayBuilder<RazorSourceDocument>(importItems.Length);

        foreach (var importItem in importItems)
        {
            if (importItem.Exists)
            {
                try
                {
                    // Normal import, has file paths, content etc.
                    var sourceDocument = RazorSourceDocument.ReadFrom(importItem);
                    imports.Add(sourceDocument);
                }
                catch (IOException) when (suppressExceptions)
                {
                    // Something happened when trying to read the item from disk.
                    // Catch the exception so we don't crash the editor.
                }
            }
        }

        return imports.DrainToImmutable();
    }

    private static void ConfigureDesignTimeParserOptions(RazorParserOptionsBuilder builder)
    {
        builder.SetDesignTime(true);
    }

    private static void ConfigureDesignTimeCodeGenerationOptions(RazorCodeGenerationOptionsBuilder builder)
    {
        builder.SetDesignTime(true);
        builder.SuppressChecksum = true;
        builder.SuppressMetadataAttributes = true;
    }
}
