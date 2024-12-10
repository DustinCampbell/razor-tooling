// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.ObjectPool;
using Microsoft.NET.Sdk.Razor.SourceGenerators;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal sealed partial class ProjectState
{
    private static readonly ObjectPool<Dictionary<string, ImmutableArray<string>.Builder>> s_importMapBuilderPool =
        DictionaryPool<string, ImmutableArray<string>.Builder>.Create(FilePathNormalizingComparer.Instance);

    private static readonly ImmutableDictionary<string, DocumentState> s_emptyDocuments =
        ImmutableDictionary<string, DocumentState>.Empty.WithComparers(keyComparer: FilePathNormalizingComparer.Instance);
    private static readonly ImmutableDictionary<string, ImmutableArray<string>> s_emptyImportsToRelatedDocuments =
        ImmutableDictionary<string, ImmutableArray<string>>.Empty.WithComparers(keyComparer: FilePathNormalizingComparer.Instance);

    public HostProject HostProject { get; }
    public RazorCompilerOptions CompilerOptions { get; }
    public ProjectWorkspaceState ProjectWorkspaceState { get; }

    private readonly ProjectVersions _versions;
    private readonly IProjectEngineFactoryProvider _projectEngineFactoryProvider;

    public ImmutableDictionary<string, DocumentState> Documents { get; }
    public ImmutableDictionary<string, ImmutableArray<string>> ImportsToRelatedDocuments { get; }

    private readonly object _lock = new();
    private RazorProjectEngine? _projectEngine;

    private ProjectState(
        HostProject hostProject,
        RazorCompilerOptions compilerOptions,
        ProjectWorkspaceState projectWorkspaceState,
        IProjectEngineFactoryProvider projectEngineFactoryProvider)
    {
        HostProject = hostProject;
        CompilerOptions = compilerOptions;
        ProjectWorkspaceState = projectWorkspaceState;
        _projectEngineFactoryProvider = projectEngineFactoryProvider;
        _versions = ProjectVersions.Create();

        Documents = s_emptyDocuments;
        ImportsToRelatedDocuments = s_emptyImportsToRelatedDocuments;
    }

    private ProjectState(
        ProjectState older,
        ProjectVersions versions,
        HostProject hostProject,
        ProjectWorkspaceState projectWorkspaceState,
        ImmutableDictionary<string, DocumentState> documents,
        ImmutableDictionary<string, ImmutableArray<string>> importsToRelatedDocuments,
        bool retainProjectEngine)
    {
        _versions = versions;

        _projectEngineFactoryProvider = older._projectEngineFactoryProvider;
        CompilerOptions = older.CompilerOptions;

        HostProject = hostProject;
        ProjectWorkspaceState = projectWorkspaceState;
        Documents = documents;
        ImportsToRelatedDocuments = importsToRelatedDocuments;

        if (retainProjectEngine && older._projectEngine is { } projectEngine)
        {
            _projectEngine = projectEngine;
        }
    }

    public static ProjectState Create(
        HostProject hostProject,
        RazorCompilerOptions compilerOptions = RazorCompilerOptions.None,
        ProjectWorkspaceState? projectWorkspaceState = null,
        IProjectEngineFactoryProvider? projectEngineFactoryProvider = null)
        => new(
            hostProject,
            compilerOptions,
            projectWorkspaceState ?? ProjectWorkspaceState.Default,
            projectEngineFactoryProvider ?? ProjectEngineFactories.DefaultProvider);

    public static ProjectState Create(
        HostProject hostProject,
        RazorCompilerOptions compilerOptions,
        IProjectEngineFactoryProvider projectEngineFactoryProvider)
        => Create(hostProject, compilerOptions, projectWorkspaceState: null, projectEngineFactoryProvider);

    public static ProjectState Create(
        HostProject hostProject,
        ProjectWorkspaceState projectWorkspaceState,
        IProjectEngineFactoryProvider? projectEngineFactoryProvider = null)
        => Create(hostProject, RazorCompilerOptions.None, projectWorkspaceState, projectEngineFactoryProvider);

    public static ProjectState Create(
        HostProject hostProject,
        IProjectEngineFactoryProvider projectEngineFactoryProvider)
        => Create(hostProject, RazorCompilerOptions.None, projectWorkspaceState: null, projectEngineFactoryProvider);

    public ImmutableArray<TagHelperDescriptor> TagHelpers => ProjectWorkspaceState.TagHelpers;

    public LanguageVersion CSharpLanguageVersion => ProjectWorkspaceState.CSharpLanguageVersion;

    /// <inheritdoc cref="ProjectVersions.Version"/>
    public VersionStamp Version => _versions.Version;

    /// <inheritdoc cref="ProjectVersions.Configuration"/>
    public VersionStamp ConfigurationVersion => _versions.Configuration;

    /// <inheritdoc cref="ProjectVersions.DocumentCollection"/>
    public VersionStamp DocumentCollectionVersion => _versions.DocumentCollection;

    /// <inheritdoc cref="ProjectVersions.ProjectWorkspaceState"/>
    public VersionStamp ProjectWorkspaceStateVersion => _versions.ProjectWorkspaceState;

    public VersionStamp GetLatestVersion()
        => _versions.GetLatestVersion();

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
                var useRoslynTokenizer = CompilerOptions.IsFlagSet(RazorCompilerOptions.UseRoslynTokenizer);

                return _projectEngineFactoryProvider.Create(configuration, rootDirectoryPath, builder =>
                {
                    builder.SetRootNamespace(HostProject.RootNamespace);
                    builder.SetCSharpLanguageVersion(CSharpLanguageVersion);
                    builder.SetSupportLocalizedComponentNames();
                    builder.Features.Add(new ConfigureRazorParserOptions(useRoslynTokenizer, CSharpParseOptions.Default));
                });
            }
        }
    }

    public ProjectState AddDocument(HostDocument hostDocument, TextLoader textLoader)
    {
        // Ignore attempts to 'add' a document with different data, we only
        // care about one, so it might as well be the one we have.
        if (Documents.ContainsKey(hostDocument.FilePath))
        {
            return this;
        }

        var documents = Documents.Add(hostDocument.FilePath, DocumentState.Create(hostDocument, textLoader));

        // Compute the effect on the import map
        var importTargetPaths = GetImportDocumentTargetPaths(hostDocument);
        var importsToRelatedDocuments = AddToImportsToRelatedDocuments(ImportsToRelatedDocuments, hostDocument.FilePath, importTargetPaths);

        // Now check if the updated document is an import - it's important this this happens after
        // updating the imports map.
        if (importsToRelatedDocuments.TryGetValue(hostDocument.TargetPath, out var relatedDocuments))
        {
            foreach (var relatedDocument in relatedDocuments)
            {
                documents = documents.SetItem(relatedDocument, documents[relatedDocument].WithImportsChange());
            }
        }

        return new(
            this,
            _versions.DocumentAdded(),
            HostProject,
            ProjectWorkspaceState,
            documents,
            importsToRelatedDocuments,
            retainProjectEngine: true);
    }

    public ProjectState RemoveDocument(HostDocument hostDocument)
    {
        if (!Documents.ContainsKey(hostDocument.FilePath))
        {
            return this;
        }

        var documents = Documents.Remove(hostDocument.FilePath);

        // First check if the updated document is an import. It's important that this happens
        // before updating the imports map.
        if (ImportsToRelatedDocuments.TryGetValue(hostDocument.TargetPath, out var relatedDocuments))
        {
            if (relatedDocuments is [var filePath])
            {
                var relatedDocument = documents[filePath];
                documents = documents.SetItem(filePath, relatedDocument.WithImportsChange());
            }
            else if (relatedDocuments.Length > 1)
            {
                using var updates = new PooledArrayBuilder<KeyValuePair<string, DocumentState>>(capacity: relatedDocuments.Length);

                foreach (var relatedDocument in relatedDocuments)
                {
                    var importDocument = documents[relatedDocument];
                    updates.Add(KeyValuePair.Create(relatedDocument, importDocument.WithImportsChange()));
                }

                documents = documents.SetItems(updates.ToArray());
            }
        }

        // Compute the effect on the import map
        var importTargetPaths = GetImportDocumentTargetPaths(hostDocument);
        var importsToRelatedDocuments = RemoveFromImportsToRelatedDocuments(ImportsToRelatedDocuments, hostDocument, importTargetPaths);

        return new(
            this,
            _versions.DocumentRemoved(),
            HostProject,
            ProjectWorkspaceState,
            documents,
            importsToRelatedDocuments,
            retainProjectEngine: true);
    }

    public ProjectState WithDocumentText(HostDocument hostDocument, SourceText sourceText, VersionStamp textVersion)
    {
        if (!Documents.TryGetValue(hostDocument.FilePath, out var document))
        {
            return this;
        }

        var newDocument = document.WithText(sourceText, textVersion);
        var documents = GetUpdatedDocuments(newDocument, Documents, ImportsToRelatedDocuments);

        return new(
            this,
            _versions.DocumentChanged(),
            HostProject,
            ProjectWorkspaceState,
            documents,
            ImportsToRelatedDocuments,
            retainProjectEngine: true);
    }

    public ProjectState WithDocumentText(HostDocument hostDocument, TextLoader loader)
    {
        if (!Documents.TryGetValue(hostDocument.FilePath, out var document))
        {
            return this;
        }

        var newDocument = document.WithTextLoader(loader);
        var documents = GetUpdatedDocuments(newDocument, Documents, ImportsToRelatedDocuments);

        return new(
            this,
            _versions.DocumentChanged(),
            HostProject,
            ProjectWorkspaceState,
            documents,
            ImportsToRelatedDocuments,
            retainProjectEngine: true);
    }

    private static ImmutableDictionary<string, DocumentState> GetUpdatedDocuments(
        DocumentState newDocument,
        ImmutableDictionary<string, DocumentState> documentStateMap,
        ImmutableDictionary<string, ImmutableArray<string>> importToRelatedDocumentsMap)
    {
        var hostDocument = newDocument.HostDocument;
        var relatedDocuments = importToRelatedDocumentsMap.GetValueOrDefault(hostDocument.TargetPath, defaultValue: []);

        if (relatedDocuments.IsEmpty)
        {
            // Easy case: This isn't an import. Just update the document.
            return documentStateMap.SetItem(hostDocument.FilePath, newDocument);
        }

        // OK, this an import. So, we need to update the document and its related documents.

        // First, collect the updates as KeyValuePairs.
        using var updates = new PooledArrayBuilder<KeyValuePair<string, DocumentState>>(capacity: relatedDocuments.Length + 1);

        // Add the updated document.
        updates.Add(KeyValuePair.Create(hostDocument.FilePath, newDocument));

        // Add updated related documents.
        foreach (var relatedDocumentPath in relatedDocuments)
        {
            var relatedDocument = documentStateMap[relatedDocumentPath];
            updates.Add(KeyValuePair.Create(relatedDocumentPath, relatedDocument.WithImportsChange()));
        }

        // Finally, apply the updates to the map.
        return documentStateMap.SetItems(updates.ToArray());
    }

    public ProjectState WithConfiguration(RazorConfiguration configuration)
    {
        if (HostProject.Configuration == configuration)
        {
            return this;
        }

        var updates = Documents.Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.WithConfigurationChange()));
        var documents = Documents.SetItems(updates);

        // If the host project has changed then we need to recompute the imports map
        var importsToRelatedDocuments = BuildImportsMap(documents.Values);

        var newHostProject = HostProject with { Configuration = configuration };

        return new(
            this,
            _versions.ConfigurationChanged(),
            newHostProject,
            ProjectWorkspaceState,
            documents,
            importsToRelatedDocuments,
            retainProjectEngine: false);
    }

    public ProjectState WithRootNamespace(string? rootNamespace)
    {
        if (HostProject.RootNamespace == rootNamespace)
        {
            return this;
        }

        var updates = Documents.Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.WithConfigurationChange()));
        var documents = Documents.SetItems(updates);

        // If the host project has changed then we need to recompute the imports map
        var importsToRelatedDocuments = BuildImportsMap(documents.Values);

        var newHostProject = HostProject with { RootNamespace = rootNamespace };

        return new(
            this,
            _versions.ConfigurationChanged(),
            newHostProject,
            ProjectWorkspaceState,
            documents,
            importsToRelatedDocuments,
            retainProjectEngine: false);
    }

    public ProjectState WithProjectWorkspaceState(ProjectWorkspaceState projectWorkspaceState)
    {
        if (ProjectWorkspaceState.Equals(projectWorkspaceState))
        {
            return this;
        }

        var updates = Documents.Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.WithProjectWorkspaceStateChange()));
        var documents = Documents.SetItems(updates);

        return new(
            this,
            _versions.ProjectWorkspaceStateChanged(),
            HostProject,
            projectWorkspaceState,
            documents,
            ImportsToRelatedDocuments,
            retainProjectEngine: CSharpLanguageVersion == projectWorkspaceState.CSharpLanguageVersion);
    }

    private ImmutableDictionary<string, ImmutableArray<string>> BuildImportsMap(IEnumerable<DocumentState> documents)
    {
        using var _ = s_importMapBuilderPool.GetPooledObject(out var map);

#if NET
        map.EnsureCapacity(ImportsToRelatedDocuments.Count);
#endif

        using var importTargetPaths = new PooledArrayBuilder<string>();

        var projectEngine = ProjectEngine;

        foreach (var document in documents)
        {
            if (importTargetPaths.Count > 0)
            {
                importTargetPaths.Clear();
            }

            CollectImportDocumentTargetPaths(document.HostDocument, projectEngine, ref importTargetPaths.AsRef());

            foreach (var importTargetPath in importTargetPaths)
            {
                if (!map.TryGetValue(importTargetPath, out var relatedDocuments))
                {
                    relatedDocuments = ImmutableArray.CreateBuilder<string>();
                    map.Add(importTargetPath, relatedDocuments);
                }

                relatedDocuments.Add(document.HostDocument.FilePath);
            }
        }

        return map.ToImmutableDictionary(
            keySelector: kvp => kvp.Key,
            elementSelector: kvp => kvp.Value.ToImmutable(),
            keyComparer: FilePathNormalizingComparer.Instance);
    }

    private static ImmutableDictionary<string, ImmutableArray<string>> AddToImportsToRelatedDocuments(
        ImmutableDictionary<string, ImmutableArray<string>> importsToRelatedDocuments,
        string documentFilePath,
        ImmutableArray<string> importTargetPaths)
    {
        foreach (var importTargetPath in importTargetPaths)
        {
            if (!importsToRelatedDocuments.TryGetValue(importTargetPath, out var relatedDocuments))
            {
                relatedDocuments = [];
            }

            relatedDocuments = relatedDocuments.Add(documentFilePath);
            importsToRelatedDocuments = importsToRelatedDocuments.SetItem(importTargetPath, relatedDocuments);
        }

        return importsToRelatedDocuments;
    }

    private static ImmutableDictionary<string, ImmutableArray<string>> RemoveFromImportsToRelatedDocuments(
        ImmutableDictionary<string, ImmutableArray<string>> importsToRelatedDocuments,
        HostDocument hostDocument,
        ImmutableArray<string> importTargetPaths)
    {
        foreach (var importTargetPath in importTargetPaths)
        {
            if (importsToRelatedDocuments.TryGetValue(importTargetPath, out var relatedDocuments))
            {
                relatedDocuments = relatedDocuments.Remove(hostDocument.FilePath);
                importsToRelatedDocuments = relatedDocuments.Length > 0
                    ? importsToRelatedDocuments.SetItem(importTargetPath, relatedDocuments)
                    : importsToRelatedDocuments.Remove(importTargetPath);
            }
        }

        importsToRelatedDocuments = importsToRelatedDocuments.Remove(hostDocument.TargetPath);

        return importsToRelatedDocuments;
    }

    public ImmutableArray<string> GetImportDocumentTargetPaths(HostDocument hostDocument)
    {
        using var targetPaths = new PooledArrayBuilder<string>();
        CollectImportDocumentTargetPaths(hostDocument, ProjectEngine, ref targetPaths.AsRef());

        return targetPaths.DrainToImmutable();
    }

    private static void CollectImportDocumentTargetPaths(HostDocument hostDocument, RazorProjectEngine projectEngine, ref PooledArrayBuilder<string> targetPaths)
    {
        var targetPath = hostDocument.TargetPath;
        var projectItem = projectEngine.FileSystem.GetItem(targetPath, hostDocument.FileKind);

        using var importProjectItems = new PooledArrayBuilder<RazorProjectItem>();
        projectEngine.CollectImportProjectItems(projectItem, ref importProjectItems.AsRef());

        if (importProjectItems.Count == 0)
        {
            return;
        }

        // Target path looks like `Foo\\Bar.cshtml`

        foreach (var importProjectItem in importProjectItems)
        {
            if (importProjectItem.FilePath is not string filePath)
            {
                continue;
            }

            var itemTargetPath = filePath.Replace('/', '\\').TrimStart('\\');

            if (FilePathNormalizer.AreFilePathsEquivalent(filePath, targetPath))
            {
                // We've normalized the original importItem.FilePath into the HostDocument.TargetPath. For instance, if the HostDocument.TargetPath
                // was '/_Imports.razor' it'd be normalized down into '_Imports.razor'. The purpose of this method is to get the associated document
                // paths for a given import file (_Imports.razor / _ViewImports.cshtml); therefore, an import importing itself doesn't make sense.
                continue;
            }

            targetPaths.Add(itemTargetPath);
        }
    }
}
