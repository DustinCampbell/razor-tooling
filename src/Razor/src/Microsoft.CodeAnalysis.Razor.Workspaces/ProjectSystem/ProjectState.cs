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
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.NET.Sdk.Razor.SourceGenerators;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

// Internal tracker for DefaultProjectSnapshot
internal class ProjectState
{
    private const ProjectDifference ClearConfigurationVersionMask = ProjectDifference.ConfigurationChanged;

    private const ProjectDifference ClearProjectWorkspaceStateVersionMask =
        ProjectDifference.ConfigurationChanged |
        ProjectDifference.ProjectWorkspaceStateChanged;

    private const ProjectDifference ClearDocumentCollectionVersionMask =
        ProjectDifference.ConfigurationChanged |
        ProjectDifference.DocumentAdded |
        ProjectDifference.DocumentRemoved;

    private static readonly ImmutableDictionary<string, DocumentState> s_emptyDocuments = ImmutableDictionary.Create<string, DocumentState>(FilePathNormalizingComparer.Instance);
    private static readonly ImmutableDictionary<string, ImmutableArray<string>> s_emptyImportsToRelatedDocuments = ImmutableDictionary.Create<string, ImmutableArray<string>>(FilePathNormalizingComparer.Instance);

    private readonly IProjectEngineFactoryProvider _projectEngineFactoryProvider;
    private readonly LanguageServerFeatureOptions _languageServerFeatureOptions;

    private readonly object _lock = new();
    private RazorProjectEngine? _projectEngine;

    private ProjectState(
        IProjectEngineFactoryProvider projectEngineFactoryProvider,
        LanguageServerFeatureOptions languageServerFeatureOptions,
        HostProject hostProject,
        ProjectWorkspaceState projectWorkspaceState)
    {
        _projectEngineFactoryProvider = projectEngineFactoryProvider;
        _languageServerFeatureOptions = languageServerFeatureOptions;
        HostProject = hostProject;
        ProjectWorkspaceState = projectWorkspaceState;
        Documents = s_emptyDocuments;
        ImportsToRelatedDocuments = s_emptyImportsToRelatedDocuments;
        Version = VersionStamp.Create();
        ProjectWorkspaceStateVersion = Version;
        DocumentCollectionVersion = Version;

        _lock = new object();
    }

    private ProjectState(
        ProjectState older,
        ProjectDifference difference,
        HostProject hostProject,
        ProjectWorkspaceState projectWorkspaceState,
        ImmutableDictionary<string, DocumentState> documents,
        ImmutableDictionary<string, ImmutableArray<string>> importsToRelatedDocuments)
    {
        _projectEngineFactoryProvider = older._projectEngineFactoryProvider;
        _languageServerFeatureOptions = older._languageServerFeatureOptions;
        Version = older.Version.GetNewerVersion();

        HostProject = hostProject;
        ProjectWorkspaceState = projectWorkspaceState;
        Documents = documents;
        ImportsToRelatedDocuments = importsToRelatedDocuments;

        _lock = new object();

        if ((difference & ClearDocumentCollectionVersionMask) == 0)
        {
            // Document collection hasn't changed
            DocumentCollectionVersion = older.DocumentCollectionVersion;
        }
        else
        {
            DocumentCollectionVersion = Version;
        }

        if ((difference & ClearConfigurationVersionMask) == 0 && older._projectEngine != null)
        {
            // Optimistically cache the RazorProjectEngine.
            _projectEngine = older.ProjectEngine;
            ConfigurationVersion = older.ConfigurationVersion;
        }
        else
        {
            ConfigurationVersion = Version;
        }

        if ((difference & ClearProjectWorkspaceStateVersionMask) == 0 ||
            ReferenceEquals(ProjectWorkspaceState, older.ProjectWorkspaceState) ||
            ProjectWorkspaceState.Equals(older.ProjectWorkspaceState))
        {
            // ProjectWorkspaceState hasn't changed.
            ProjectWorkspaceStateVersion = older.ProjectWorkspaceStateVersion;
        }
        else
        {
            ProjectWorkspaceStateVersion = Version;
        }

        if ((difference & ClearProjectWorkspaceStateVersionMask) != 0 &&
            CSharpLanguageVersion != older.CSharpLanguageVersion)
        {
            // C# language version changed. This impacts the ProjectEngine, reset it.
            _projectEngine = null;
            ConfigurationVersion = Version;
        }
    }

    public static ProjectState Create(
        IProjectEngineFactoryProvider projectEngineFactoryProvider,
        LanguageServerFeatureOptions languageServerFeatureOptions,
        HostProject hostProject,
        ProjectWorkspaceState projectWorkspaceState)
    {
        return new(projectEngineFactoryProvider, languageServerFeatureOptions, hostProject, projectWorkspaceState);
    }

    // Internal set for testing.
    public ImmutableDictionary<string, DocumentState> Documents { get; internal set; }

    // Internal set for testing.
    public ImmutableDictionary<string, ImmutableArray<string>> ImportsToRelatedDocuments { get; internal set; }

    public HostProject HostProject { get; }

    internal LanguageServerFeatureOptions LanguageServerFeatureOptions => _languageServerFeatureOptions;

    public ProjectWorkspaceState ProjectWorkspaceState { get; }

    public ImmutableArray<TagHelperDescriptor> TagHelpers => ProjectWorkspaceState.TagHelpers;

    public LanguageVersion CSharpLanguageVersion => ProjectWorkspaceState.CSharpLanguageVersion;

    /// <summary>
    /// Gets the version of this project, INCLUDING content changes. The <see cref="Version"/> is
    /// incremented for each new <see cref="ProjectState"/> instance created.
    /// </summary>
    public VersionStamp Version { get; }

    /// <summary>
    /// Gets the version of this project, NOT INCLUDING computed or content changes. The
    /// <see cref="DocumentCollectionVersion"/> is incremented each time the configuration changes or
    /// a document is added or removed.
    /// </summary>
    public VersionStamp DocumentCollectionVersion { get; }

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
                var useRoslynTokenizer = LanguageServerFeatureOptions.UseRoslynTokenizer;

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

    public VersionStamp GetLatestVersion()
    {
        VersionStamp result = default;

        result = result.GetNewerVersion(ConfigurationVersion);
        result = result.GetNewerVersion(ProjectWorkspaceStateVersion);
        result = result.GetNewerVersion(DocumentCollectionVersion);

        return result;
    }

    /// <summary>
    /// Gets the version of this project based on the project workspace state, NOT INCLUDING content
    /// changes. The computed state is guaranteed to change when the configuration or tag helpers
    /// change.
    /// </summary>
    public VersionStamp ProjectWorkspaceStateVersion { get; }

    public VersionStamp ConfigurationVersion { get; }

    public ProjectState WithAddedHostDocument(HostDocument hostDocument, TextLoader textLoader)
    {
        ArgHelper.ThrowIfNull(hostDocument);
        ArgHelper.ThrowIfNull(textLoader);

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

        return new(this, ProjectDifference.DocumentAdded, HostProject, ProjectWorkspaceState, documents, importsToRelatedDocuments);
    }

    public ProjectState WithRemovedHostDocument(HostDocument hostDocument)
    {
        ArgHelper.ThrowIfNull(hostDocument);

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

        return new(this, ProjectDifference.DocumentRemoved, HostProject, ProjectWorkspaceState, documents, importsToRelatedDocuments);
    }

    public ProjectState WithChangedHostDocument(HostDocument hostDocument, SourceText sourceText, VersionStamp textVersion)
    {
        ArgHelper.ThrowIfNull(hostDocument);

        if (!Documents.TryGetValue(hostDocument.FilePath, out var document))
        {
            return this;
        }

        var newDocument = document.WithText(sourceText, textVersion);
        var documents = GetUpdatedDocuments(newDocument, Documents, ImportsToRelatedDocuments);

        return new(this, ProjectDifference.DocumentChanged, HostProject, ProjectWorkspaceState, documents, ImportsToRelatedDocuments);
    }

    public ProjectState WithChangedHostDocument(HostDocument hostDocument, TextLoader loader)
    {
        ArgHelper.ThrowIfNull(hostDocument);

        if (!Documents.TryGetValue(hostDocument.FilePath, out var document))
        {
            return this;
        }

        var newDocument = document.WithTextLoader(loader);
        var documents = GetUpdatedDocuments(newDocument, Documents, ImportsToRelatedDocuments);

        return new(this, ProjectDifference.DocumentChanged, HostProject, ProjectWorkspaceState, documents, ImportsToRelatedDocuments);
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

    public ProjectState WithHostProject(HostProject hostProject)
    {
        ArgHelper.ThrowIfNull(hostProject);

        if (HostProject.Configuration == hostProject.Configuration &&
            HostProject.RootNamespace == hostProject.RootNamespace)
        {
            return this;
        }

        var newDocuments = Documents.Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.WithConfigurationChange()));
        var documents = Documents.SetItems(newDocuments);

        // If the host project has changed then we need to recompute the imports map
        var importsToRelatedDocuments = s_emptyImportsToRelatedDocuments;

        foreach (var document in documents)
        {
            var importTargetPaths = GetImportDocumentTargetPaths(document.Value.HostDocument);
            importsToRelatedDocuments = AddToImportsToRelatedDocuments(importsToRelatedDocuments, document.Value.HostDocument.FilePath, importTargetPaths);
        }

        return new(this, ProjectDifference.ConfigurationChanged, hostProject, ProjectWorkspaceState, documents, importsToRelatedDocuments);
    }

    public ProjectState WithProjectWorkspaceState(ProjectWorkspaceState projectWorkspaceState)
    {
        if (ReferenceEquals(ProjectWorkspaceState, projectWorkspaceState) ||
            ProjectWorkspaceState.Equals(projectWorkspaceState))
        {
            return this;
        }

        var newDocuments = Documents.Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.WithProjectWorkspaceStateChange()));
        var documents = Documents.SetItems(newDocuments);

        return new(this, ProjectDifference.ProjectWorkspaceStateChanged, HostProject, projectWorkspaceState, documents, ImportsToRelatedDocuments);
    }

    internal static ImmutableDictionary<string, ImmutableArray<string>> AddToImportsToRelatedDocuments(
        ImmutableDictionary<string, ImmutableArray<string>> importsToRelatedDocuments,
        string documentFilePath,
        List<string> importTargetPaths)
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
        List<string> importTargetPaths)
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

        return importsToRelatedDocuments;
    }

    public List<string> GetImportDocumentTargetPaths(HostDocument hostDocument)
    {
        return GetImportDocumentTargetPaths(hostDocument.TargetPath, hostDocument.FileKind, ProjectEngine);
    }

    internal static List<string> GetImportDocumentTargetPaths(string targetPath, string fileKind, RazorProjectEngine projectEngine)
    {
        var importFeatures = projectEngine.ProjectFeatures.OfType<IImportProjectFeature>();
        var projectItem = projectEngine.FileSystem.GetItem(targetPath, fileKind);
        var importItems = importFeatures.SelectMany(f => f.GetImports(projectItem)).Where(i => i.FilePath != null);

        // Target path looks like `Foo\\Bar.cshtml`
        var targetPaths = new List<string>();
        foreach (var importItem in importItems)
        {
            var itemTargetPath = importItem.FilePath.Replace('/', '\\').TrimStart('\\');

            if (FilePathNormalizingComparer.Instance.Equals(itemTargetPath, targetPath))
            {
                // We've normalized the original importItem.FilePath into the HostDocument.TargetPath. For instance, if the HostDocument.TargetPath
                // was '/_Imports.razor' it'd be normalized down into '_Imports.razor'. The purpose of this method is to get the associated document
                // paths for a given import file (_Imports.razor / _ViewImports.cshtml); therefore, an import importing itself doesn't make sense.
                continue;
            }

            targetPaths.Add(itemTargetPath);
        }

        return targetPaths;
    }
}
