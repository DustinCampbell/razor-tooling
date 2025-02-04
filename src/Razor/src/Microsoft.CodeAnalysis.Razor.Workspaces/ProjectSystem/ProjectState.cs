// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.ObjectPool;
using Microsoft.NET.Sdk.Razor.SourceGenerators;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal sealed class ProjectState
{
    private static readonly ObjectPool<Dictionary<string, ImmutableHashSet<string>.Builder>> s_importMapBuilderPool =
        DictionaryPool<string, ImmutableHashSet<string>.Builder>.Create(FilePathNormalizingComparer.Instance);

    private static readonly ImmutableDictionary<string, DocumentState> s_emptyDocuments
        = ImmutableDictionary.Create<string, DocumentState>(FilePathNormalizingComparer.Instance);
    private static readonly ImmutableDictionary<string, ImmutableHashSet<string>> s_emptyImportsToRelatedDocuments
        = ImmutableDictionary.Create<string, ImmutableHashSet<string>>(FilePathNormalizingComparer.Instance);
    private static readonly ImmutableHashSet<string> s_emptyRelatedDocuments
        = ImmutableHashSet.Create<string>(FilePathNormalizingComparer.Instance);

    private readonly object _lock = new();

    public HostProject HostProject { get; }
    public RazorCompilerOptions CompilerOptions { get; }
    public ProjectWorkspaceState ProjectWorkspaceState { get; }

    public ImmutableDictionary<string, DocumentState> Documents { get; }
    public ImmutableDictionary<string, ImmutableHashSet<string>> ImportsToRelatedDocuments { get; }

    private readonly IProjectEngineFactoryProvider _projectEngineFactoryProvider;
    private readonly ILogger _logger;
    private RazorProjectEngine? _projectEngine;

    private ProjectState(
        HostProject hostProject,
        RazorCompilerOptions compilerOptions,
        IProjectEngineFactoryProvider projectEngineFactoryProvider,
        ILogger logger)
    {
        HostProject = hostProject;
        ProjectWorkspaceState = ProjectWorkspaceState.Default;
        CompilerOptions = compilerOptions;
        _projectEngineFactoryProvider = projectEngineFactoryProvider;
        _logger = logger;

        Documents = s_emptyDocuments;
        ImportsToRelatedDocuments = s_emptyImportsToRelatedDocuments;
    }

    private ProjectState(
        ProjectState older,
        HostProject hostProject,
        ProjectWorkspaceState projectWorkspaceState,
        ImmutableDictionary<string, DocumentState> documents,
        ImmutableDictionary<string, ImmutableHashSet<string>> importsToRelatedDocuments,
        bool retainProjectEngine)
    {
        HostProject = hostProject;
        CompilerOptions = older.CompilerOptions;
        _projectEngineFactoryProvider = older._projectEngineFactoryProvider;
        _logger = older._logger;
        ProjectWorkspaceState = projectWorkspaceState;

        Documents = documents;
        ImportsToRelatedDocuments = importsToRelatedDocuments;

        if (retainProjectEngine)
        {
            _projectEngine = older._projectEngine;
        }
    }

    public static ProjectState Create(
        HostProject hostProject,
        RazorCompilerOptions compilerOptions,
        IProjectEngineFactoryProvider projectEngineFactoryProvider,
        ILogger? logger = null)
        => new(hostProject, compilerOptions, projectEngineFactoryProvider, logger ?? EmptyLoggerFactory.Instance.GetOrCreateLogger("ProjectState"));

    public ImmutableArray<TagHelperDescriptor> TagHelpers => ProjectWorkspaceState.TagHelpers;

    public LanguageVersion CSharpLanguageVersion => HostProject.Configuration.CSharpLanguageVersion;

    public RazorProjectEngine ProjectEngine
    {
        get
        {
            lock (_lock)
            {
                _projectEngine ??= CreateProjectEngine();
            }

            return _projectEngine;

            RazorProjectEngine CreateProjectEngine()
            {
                var configuration = HostProject.Configuration;
                var rootDirectoryPath = Path.GetDirectoryName(HostProject.FilePath).AssumeNotNull();
                var useRoslynTokenizer = configuration.UseRoslynTokenizer;
                var parseOptions = new CSharpParseOptions(languageVersion: CSharpLanguageVersion, preprocessorSymbols: configuration.PreprocessorSymbols);

                return _projectEngineFactoryProvider.Create(configuration, rootDirectoryPath, builder =>
                {
                    builder.SetRootNamespace(HostProject.RootNamespace);
                    builder.SetCSharpLanguageVersion(CSharpLanguageVersion);
                    builder.SetSupportLocalizedComponentNames();
                    builder.Features.Add(new ConfigureRazorParserOptions(useRoslynTokenizer, parseOptions));
                });
            }
        }
    }

    public ProjectState AddEmptyDocument(HostDocument hostDocument)
        => AddDocument(hostDocument, EmptyTextLoader.Instance);

    public ProjectState AddDocument(HostDocument hostDocument, SourceText text)
    {
        ArgHelper.ThrowIfNull(hostDocument);
        ArgHelper.ThrowIfNull(text);

        // Ignore attempts to 'add' a document with different data, we only
        // care about one, so it might as well be the one we have.
        if (Documents.ContainsKey(hostDocument.FilePath))
        {
            return this;
        }

        var projectEngine = ProjectEngine;
        var projectItem = projectEngine.FileSystem.GetItem(hostDocument.TargetPath);
        var properties = RazorSourceDocumentProperties.Create(hostDocument.FilePath, projectItem.RelativePhysicalPath);

        var state = DocumentState.Create(hostDocument, properties, text);

        return AddDocument(state);
    }

    public ProjectState AddDocument(HostDocument hostDocument, TextLoader textLoader)
    {
        ArgHelper.ThrowIfNull(hostDocument);
        ArgHelper.ThrowIfNull(textLoader);

        // Ignore attempts to 'add' a document with different data, we only
        // care about one, so it might as well be the one we have.
        if (Documents.ContainsKey(hostDocument.FilePath))
        {
            return this;
        }

        var projectEngine = ProjectEngine;
        var projectItem = projectEngine.FileSystem.GetItem(hostDocument.TargetPath);
        var properties = RazorSourceDocumentProperties.Create(hostDocument.FilePath, projectItem.RelativePhysicalPath);

        var state = DocumentState.Create(hostDocument, properties, textLoader);

        return AddDocument(state);
    }

    private ProjectState AddDocument(DocumentState state)
    {
        var hostDocument = state.HostDocument;
        var documents = Documents.Add(hostDocument.FilePath, state);

        // Compute the effect on the import map
        var importsToRelatedDocuments = AddToImportsToRelatedDocuments(hostDocument);

        // Then, if this is an import, update any related documents.
        documents = UpdateRelatedDocumentsIfNecessary(hostDocument, documents);

        return new(this, HostProject, ProjectWorkspaceState, documents, importsToRelatedDocuments, retainProjectEngine: true);
    }

    public ProjectState RemoveDocument(string documentFilePath)
    {
        ArgHelper.ThrowIfNull(documentFilePath);

        if (!Documents.TryGetValue(documentFilePath, out var state))
        {
            return this;
        }

        var hostDocument = state.HostDocument;

        var documents = Documents.Remove(documentFilePath);

        // If this is an import, update any related documents.
        documents = UpdateRelatedDocumentsIfNecessary(hostDocument, documents);

        // Then, compute the effect on the import map
        var importsToRelatedDocuments = RemoveFromImportsToRelatedDocuments(hostDocument);

        return new(this, HostProject, ProjectWorkspaceState, documents, importsToRelatedDocuments, retainProjectEngine: true);
    }

    public ProjectState WithDocumentText(string documentFilePath, SourceText text)
    {
        ArgHelper.ThrowIfNull(documentFilePath);
        ArgHelper.ThrowIfNull(text);

        if (!Documents.TryGetValue(documentFilePath, out var oldState))
        {
            return this;
        }

        return WithDocumentText(oldState, state => state.WithText(text));
    }

    public ProjectState WithDocumentText(string documentFilePath, TextLoader textLoader)
    {
        ArgHelper.ThrowIfNull(documentFilePath);

        if (!Documents.TryGetValue(documentFilePath, out var state))
        {
            return this;
        }

        return WithDocumentText(state, state => state.WithTextLoader(textLoader));
    }

    private ProjectState WithDocumentText(DocumentState state, Func<DocumentState, DocumentState> transformer)
    {
        var newState = transformer(state);

        if (ReferenceEquals(this, newState))
        {
            return this;
        }

        var hostDocument = state.HostDocument;
        var documents = Documents.SetItem(hostDocument.FilePath, newState);

        // If this document is an import, update its related documents.
        documents = UpdateRelatedDocumentsIfNecessary(hostDocument, documents);

        return new(this, HostProject, ProjectWorkspaceState, documents, ImportsToRelatedDocuments, retainProjectEngine: true);
    }

    public ProjectState WithHostProject(HostProject hostProject)
    {
        ArgHelper.ThrowIfNull(hostProject);

        if (HostProject.Configuration == hostProject.Configuration &&
            HostProject.RootNamespace == hostProject.RootNamespace)
        {
            return this;
        }

        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        builder.AppendLine($"{HostProject.DisplayName}: Invalidating {Documents.Count} document(s).");

        if (HostProject.FilePath != hostProject.FilePath)
        {
            builder.AppendLine("- FilePath changed.");
        }

        if (HostProject.IntermediateOutputPath != hostProject.IntermediateOutputPath)
        {
            builder.AppendLine("- IntermediateOutputPath changed.");
        }

        if (HostProject.Configuration != hostProject.Configuration)
        {
            builder.AppendLine("- Configuration changed.");
        }

        if (HostProject.RootNamespace != hostProject.RootNamespace)
        {
            builder.AppendLine("- RootNamespace changed.");
        }

        if (HostProject.DisplayName != hostProject.DisplayName)
        {
            builder.AppendLine("- DisplayName changed.");
        }

        _logger.LogInformation(builder.ToString());

        var documents = UpdateDocuments(static x => x.WithConfigurationChange());

        // If the host project has changed then we need to recompute the imports map
        var importsToRelatedDocuments = BuildImportsMap(documents.Values, ProjectEngine);

        return new(this, hostProject, ProjectWorkspaceState, documents, importsToRelatedDocuments, retainProjectEngine: false);
    }

    public ProjectState WithProjectWorkspaceState(ProjectWorkspaceState projectWorkspaceState)
    {
        ArgHelper.ThrowIfNull(projectWorkspaceState);

        if (ProjectWorkspaceState == projectWorkspaceState ||
            ProjectWorkspaceState.Equals(projectWorkspaceState))
        {
            return this;
        }

        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        builder.AppendLine($"{HostProject.DisplayName}: Invalidating {Documents.Count} document(s).");
        builder.AppendLine($"- ProjectWorkspaceState.TagHelpers changed from {ProjectWorkspaceState.TagHelpers.Length} to {projectWorkspaceState.TagHelpers.Length} tag helper(s).");

        _logger.LogInformation(builder.ToString());

        var documents = UpdateDocuments(static x => x.WithProjectWorkspaceStateChange());

        return new(this, HostProject, projectWorkspaceState, documents, ImportsToRelatedDocuments, retainProjectEngine: true);
    }

    private ImmutableDictionary<string, ImmutableHashSet<string>> AddToImportsToRelatedDocuments(HostDocument hostDocument)
    {
        using var importTargetPaths = new PooledArrayBuilder<string>();
        CollectImportDocumentTargetPaths(hostDocument, ProjectEngine, ref importTargetPaths.AsRef());

        if (importTargetPaths.Count == 0)
        {
            return ImportsToRelatedDocuments;
        }

        using var _ = ListPool<KeyValuePair<string, ImmutableHashSet<string>>>.GetPooledObject(out var updates);

        var importsToRelatedDocuments = ImportsToRelatedDocuments;

        foreach (var importTargetPath in importTargetPaths)
        {
            if (!importsToRelatedDocuments.TryGetValue(importTargetPath, out var relatedDocuments))
            {
                relatedDocuments = [];
            }

            updates.Add(KeyValuePair.Create(importTargetPath, relatedDocuments.Add(hostDocument.FilePath)));
        }

        if (updates.Count > 0)
        {
            importsToRelatedDocuments = importsToRelatedDocuments.SetItems(updates);
        }

        return importsToRelatedDocuments;
    }

    private ImmutableDictionary<string, ImmutableHashSet<string>> RemoveFromImportsToRelatedDocuments(HostDocument hostDocument)
    {
        using var importTargetPaths = new PooledArrayBuilder<string>();
        CollectImportDocumentTargetPaths(hostDocument, ProjectEngine, ref importTargetPaths.AsRef());

        if (importTargetPaths.Count == 0)
        {
            return ImportsToRelatedDocuments;
        }

        using var _1 = ListPool<string>.GetPooledObject(out var removes);
        using var _2 = ListPool<KeyValuePair<string, ImmutableHashSet<string>>>.GetPooledObject(out var updates);

        var importsToRelatedDocuments = ImportsToRelatedDocuments;

        foreach (var importTargetPath in importTargetPaths)
        {
            if (importsToRelatedDocuments.TryGetValue(importTargetPath, out var relatedDocuments))
            {
                if (relatedDocuments.Count == 1)
                {
                    removes.Add(importTargetPath);
                }
                else
                {
                    updates.Add(KeyValuePair.Create(importTargetPath, relatedDocuments.Remove(hostDocument.FilePath)));
                }
            }
        }

        if (updates.Count > 0)
        {
            importsToRelatedDocuments = importsToRelatedDocuments.SetItems(updates);
        }

        if (removes.Count > 0)
        {
            importsToRelatedDocuments = importsToRelatedDocuments.RemoveRange(removes);
        }

        return importsToRelatedDocuments;
    }

    public ImmutableArray<string> GetImportDocumentTargetPaths(HostDocument hostDocument)
    {
        using var importTargetPaths = new PooledArrayBuilder<string>();
        CollectImportDocumentTargetPaths(hostDocument, ProjectEngine, ref importTargetPaths.AsRef());

        return importTargetPaths.DrainToImmutable();
    }

    private ImmutableDictionary<string, DocumentState> UpdateDocuments(Func<DocumentState, DocumentState> transformer)
    {
        var updates = Documents.Select(x => KeyValuePair.Create(x.Key, transformer(x.Value)));
        return Documents.SetItems(updates);
    }

    private ImmutableDictionary<string, DocumentState> UpdateRelatedDocumentsIfNecessary(HostDocument hostDocument, ImmutableDictionary<string, DocumentState> documents)
    {
        if (!ImportsToRelatedDocuments.TryGetValue(hostDocument.TargetPath, out var relatedDocuments))
        {
            return documents;
        }

        var updates = relatedDocuments.Select(documentFilePath =>
        {
            var document = documents[documentFilePath];

            VersionStamp? importVersion = TryComputeLatestImportsVersion(document.HostDocument, out var version)
                ? version
                : null;

            return KeyValuePair.Create(documentFilePath, document.WithImportsChange(importVersion));
        });

        return documents.SetItems(updates);
    }

    private static ImmutableDictionary<string, ImmutableHashSet<string>> BuildImportsMap(IEnumerable<DocumentState> documents, RazorProjectEngine projectEngine)
    {
        using var _ = s_importMapBuilderPool.GetPooledObject(out var map);

        using var importTargetPaths = new PooledArrayBuilder<string>();

        foreach (var document in documents)
        {
            if (importTargetPaths.Count > 0)
            {
                importTargetPaths.Clear();
            }

            var hostDocument = document.HostDocument;

            CollectImportDocumentTargetPaths(hostDocument, projectEngine, ref importTargetPaths.AsRef());

            foreach (var importTargetPath in importTargetPaths)
            {
                if (!map.TryGetValue(importTargetPath, out var relatedDocuments))
                {
                    relatedDocuments = s_emptyRelatedDocuments.ToBuilder();
                    map.Add(importTargetPath, relatedDocuments);
                }

                relatedDocuments.Add(hostDocument.FilePath);
            }
        }

        return map
            .Select(static x => KeyValuePair.Create(x.Key, x.Value.ToImmutable()))
            .ToImmutableDictionary(FilePathNormalizingComparer.Instance);
    }

    /// <summary>
    ///  Computes the latest version of the applicable imports for the given <see cref="HostDocument"/>.
    /// </summary>
    public bool TryComputeLatestImportsVersion(HostDocument hostDocument, out VersionStamp version)
    {
        version = default;

        var targetPath = hostDocument.TargetPath;
        var projectEngine = ProjectEngine;

        var projectItem = projectEngine.FileSystem.GetItem(targetPath, hostDocument.FileKind);

        using var importProjectItems = new PooledArrayBuilder<RazorProjectItem>();
        projectEngine.CollectImports(projectItem, ref importProjectItems.AsRef());

        if (importProjectItems.Count == 0)
        {
            return false;
        }

        foreach (var importProjectItem in importProjectItems)
        {
            if (importProjectItem is NotFoundProjectItem or DefaultImportProjectItem)
            {
                continue;
            }

            if (Documents.TryGetValue(importProjectItem.PhysicalPath, out var document))
            {
                // If an import document's source hasn't been loaded yet, we can't compute the version.
                if (!document.TryGetTextVersion(out var importVersion))
                {
                    version = default;
                    return false;
                }

                version = version.GetNewerVersion(importVersion);
            }
        }

        return version != default;
    }

    /// <summary>
    ///  Collects the applicable import document target paths for the given <see cref="HostDocument"/>
    ///  in <paramref name="importTargetPaths"/>
    /// </summary>
    private static void CollectImportDocumentTargetPaths(
        HostDocument hostDocument,
        RazorProjectEngine projectEngine,
        ref PooledArrayBuilder<string> importTargetPaths)
    {
        var targetPath = hostDocument.TargetPath;
        var projectItem = projectEngine.FileSystem.GetItem(targetPath, hostDocument.FileKind);

        using var importProjectItems = new PooledArrayBuilder<RazorProjectItem>();
        projectEngine.CollectImports(projectItem, ref importProjectItems.AsRef());

        if (importProjectItems.Count == 0)
        {
            return;
        }

        // Razor's "target path" takes the form of 'Components\Views\Error.razor' in Visual Studio.

        foreach (var importProjectItem in importProjectItems)
        {
            // Note: We skip default imports because they can't change and never manifest on disk.
            if (importProjectItem is NotFoundProjectItem or DefaultImportProjectItem)
            {
                continue;
            }

            // RazorProjectItem.FilePath is defined as relative to the project root with a leading '/'
            // and all other slashes normalized to '/'.
            //
            // Given that, we prefer RazorProjectItem.RelativePhysicalPath, which should be equivalent
            // to Razor's target path.

            var importTargetPath = importProjectItem.RelativePhysicalPath;

            if (importTargetPath.IsNullOrEmpty())
            {
                // If RazorProjectItem.RelativePhysicalPath wasn't provided, we can construct one from
                // RazorProjectItem.FilePath.
                importTargetPath = importProjectItem.GetTargetPathFromFilePath();

                if (importTargetPath.IsNullOrEmpty())
                {
                    continue;
                }
            }

            if (FilePathNormalizer.AreFilePathsEquivalent(importTargetPath, targetPath))
            {
                // The purpose of this method is to get the associated import document
                // paths (i.e. _Imports.razor / _ViewImports.cshtml) for a given document.
                // Therefore, we can skip the document itself if it *is* an import.
                continue;
            }

            importTargetPaths.Add(importTargetPath);
        }
    }
}
