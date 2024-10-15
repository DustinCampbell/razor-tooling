// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

// Internal tracker for DefaultProjectSnapshot
internal sealed class ProjectState
{
    private const ProjectDifference ClearConfigurationVersionMask = ProjectDifference.ConfigurationChanged;

    private const ProjectDifference ClearProjectWorkspaceStateVersionMask =
        ProjectDifference.ConfigurationChanged |
        ProjectDifference.ProjectWorkspaceStateChanged;

    private const ProjectDifference ClearDocumentCollectionVersionMask =
        ProjectDifference.ConfigurationChanged |
        ProjectDifference.DocumentAdded |
        ProjectDifference.DocumentRemoved;

    private static readonly ImmutableDictionary<string, DocumentState> s_emptyDocuments
        = ImmutableDictionary.Create<string, DocumentState>(FilePathNormalizingComparer.Instance);
    private static readonly ImmutableDictionary<string, ImmutableArray<string>> s_emptyImportsToRelatedDocuments
        = ImmutableDictionary.Create<string, ImmutableArray<string>>(FilePathNormalizingComparer.Instance);

    private readonly object _lock = new();

    private readonly IProjectEngineFactoryProvider _projectEngineFactoryProvider;
    private RazorProjectEngine? _projectEngine;

    public static ProjectState Create(
        IProjectEngineFactoryProvider projectEngineFactoryProvider,
        HostProject hostProject,
        ProjectWorkspaceState projectWorkspaceState)
    {
        return new ProjectState(projectEngineFactoryProvider, hostProject, projectWorkspaceState);
    }

    private ProjectState(
        IProjectEngineFactoryProvider projectEngineFactoryProvider,
        HostProject hostProject,
        ProjectWorkspaceState projectWorkspaceState)
    {
        _projectEngineFactoryProvider = projectEngineFactoryProvider;
        HostProject = hostProject;
        ProjectWorkspaceState = projectWorkspaceState;
        Documents = s_emptyDocuments;
        ImportsToRelatedDocuments = s_emptyImportsToRelatedDocuments;
        Version = VersionStamp.Create();
        ProjectWorkspaceStateVersion = Version;
        DocumentCollectionVersion = Version;
    }

    private ProjectState(
        ProjectState oldState,
        VersionStamp newVersion,
        ProjectDifference difference,
        HostProject hostProject,
        ProjectWorkspaceState projectWorkspaceState,
        ImmutableDictionary<string, DocumentState> documents,
        ImmutableDictionary<string, ImmutableArray<string>> importsToRelatedDocuments)
    {
        _projectEngineFactoryProvider = oldState._projectEngineFactoryProvider;
        Version = newVersion;

        HostProject = hostProject;
        ProjectWorkspaceState = projectWorkspaceState;
        Documents = documents;
        ImportsToRelatedDocuments = importsToRelatedDocuments;

        DocumentCollectionVersion = difference.IsFlagClear(ClearDocumentCollectionVersionMask)
            ? oldState.DocumentCollectionVersion
            : Version;

        if (difference.IsFlagClear(ClearConfigurationVersionMask) && oldState._projectEngine != null)
        {
            // Optimistically cache the RazorProjectEngine.
            _projectEngine = oldState.ProjectEngine;
            ConfigurationVersion = oldState.ConfigurationVersion;
        }
        else
        {
            ConfigurationVersion = Version;
        }

        if (difference.IsFlagClear(ClearProjectWorkspaceStateVersionMask) ||
            ProjectWorkspaceState == oldState.ProjectWorkspaceState)
        {
            ProjectWorkspaceStateVersion = oldState.ProjectWorkspaceStateVersion;
        }
        else
        {
            ProjectWorkspaceStateVersion = Version;
        }

        if (!difference.IsFlagClear(ClearProjectWorkspaceStateVersionMask) &&
            CSharpLanguageVersion != oldState.CSharpLanguageVersion)
        {
            // C# language version changed. This impacts the ProjectEngine, reset it.
            _projectEngine = null;
            ConfigurationVersion = Version;
        }
    }

    // Internal set for testing.
    public ImmutableDictionary<string, DocumentState> Documents { get; internal set; }

    // Internal set for testing.
    public ImmutableDictionary<string, ImmutableArray<string>> ImportsToRelatedDocuments { get; internal set; }

    public HostProject HostProject { get; }

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

                return _projectEngineFactoryProvider.Create(configuration, rootDirectoryPath, builder =>
                {
                    builder.SetRootNamespace(HostProject.RootNamespace);
                    builder.SetCSharpLanguageVersion(CSharpLanguageVersion);
                    builder.SetSupportLocalizedComponentNames();
                });
            }
        }
    }

    /// <summary>
    /// Gets the version of this project based on the project workspace state, NOT INCLUDING content
    /// changes. The computed state is guaranteed to change when the configuration or tag helpers
    /// change.
    /// </summary>
    public VersionStamp ProjectWorkspaceStateVersion { get; }

    public VersionStamp ConfigurationVersion { get; }

    public ProjectState WithAddedHostDocument(HostDocument hostDocument, TextLoader loader)
    {
        // Ignore attempts to 'add' a document with different data, we only
        // care about one, so it might as well be the one we have.
        if (Documents.ContainsKey(hostDocument.FilePath))
        {
            return this;
        }

        var documents = Documents.Add(hostDocument.FilePath, DocumentState.Create(hostDocument, version: 1, loader));

        // Compute the effect on the import map
        var importTargetPaths = GetImportDocumentTargetPaths(hostDocument);
        var importsToRelatedDocuments = AddToImportsToRelatedDocuments(ImportsToRelatedDocuments, hostDocument.FilePath, importTargetPaths);

        // Now check if the updated document is an import - it's important this this happens after
        // updating the imports map.
        documents = UpdateImportsIfNecessary(hostDocument, importsToRelatedDocuments, documents);

        return Update(ProjectDifference.DocumentAdded, HostProject, ProjectWorkspaceState, documents, importsToRelatedDocuments);
    }

    public ProjectState WithRemovedHostDocument(HostDocument hostDocument)
    {
        if (!Documents.ContainsKey(hostDocument.FilePath))
        {
            return this;
        }

        var documents = Documents.Remove(hostDocument.FilePath);

        // First check if the updated document is an import - it's important that this happens
        // before updating the imports map.
        documents = UpdateImportsIfNecessary(hostDocument, ImportsToRelatedDocuments, documents);

        // Compute the effect on the import map
        var importTargetPaths = GetImportDocumentTargetPaths(hostDocument);
        var importsToRelatedDocuments = RemoveFromImportsToRelatedDocuments(ImportsToRelatedDocuments, hostDocument, importTargetPaths);

        return Update(ProjectDifference.DocumentRemoved, HostProject, ProjectWorkspaceState, documents, importsToRelatedDocuments);
    }

    public ProjectState WithChangedHostDocument(HostDocument hostDocument, SourceText sourceText, VersionStamp textVersion)
    {
        if (!Documents.TryGetValue(hostDocument.FilePath, out var document))
        {
            return this;
        }

        var documents = Documents.SetItem(hostDocument.FilePath, document.WithText(sourceText, textVersion));

        documents = UpdateImportsIfNecessary(hostDocument, ImportsToRelatedDocuments, documents);

        return Update(ProjectDifference.DocumentChanged, HostProject, ProjectWorkspaceState, documents, ImportsToRelatedDocuments);
    }

    public ProjectState WithChangedHostDocument(HostDocument hostDocument, TextLoader loader)
    {
        if (!Documents.TryGetValue(hostDocument.FilePath, out var document))
        {
            return this;
        }

        var documents = Documents.SetItem(hostDocument.FilePath, document.WithTextLoader(loader));

        documents = UpdateImportsIfNecessary(hostDocument, ImportsToRelatedDocuments, documents);

        return Update(ProjectDifference.DocumentChanged, HostProject, ProjectWorkspaceState, documents, ImportsToRelatedDocuments);
    }

    private static ImmutableDictionary<string, DocumentState> UpdateImportsIfNecessary(
        HostDocument hostDocument,
        ImmutableDictionary<string, ImmutableArray<string>> importsToRelatedDocumentsMap,
        ImmutableDictionary<string, DocumentState> pathToDocumentMap)
    {
        if (!importsToRelatedDocumentsMap.TryGetValue(hostDocument.TargetPath, out var relatedDocuments))
        {
            return pathToDocumentMap;
        }
        
        var updates = relatedDocuments.Select(relatedDocument => new KeyValuePair<string, DocumentState>(relatedDocument, pathToDocumentMap[relatedDocument].WithImportsChange()));
        return pathToDocumentMap.SetItems(updates);
    }

    public ProjectState WithHostProject(HostProject hostProject)
    {
        if (HostProject.Configuration == hostProject.Configuration &&
            HostProject.RootNamespace == hostProject.RootNamespace)
        {
            return this;
        }

        var updates = Documents.Select(WithConfigurationChange);
        var documents = Documents.SetItems(updates);

        // If the host project has changed then we need to recompute the imports map
        var importsToRelatedDocuments = s_emptyImportsToRelatedDocuments;

        foreach (var document in documents)
        {
            var importTargetPaths = GetImportDocumentTargetPaths(document.Value.HostDocument);
            importsToRelatedDocuments = AddToImportsToRelatedDocuments(importsToRelatedDocuments, document.Value.HostDocument.FilePath, importTargetPaths);
        }

        return Update(ProjectDifference.ConfigurationChanged, hostProject, ProjectWorkspaceState, documents, importsToRelatedDocuments);

        static KeyValuePair<string, DocumentState> WithConfigurationChange(KeyValuePair<string, DocumentState> pair)
        {
            return new(pair.Key, pair.Value.WithConfigurationChange());
        }
    }

    public ProjectState WithProjectWorkspaceState(ProjectWorkspaceState projectWorkspaceState)
    {
        if (ProjectWorkspaceState == projectWorkspaceState)
        {
            return this;
        }

        var updates = Documents.Select(WithProjectWorkspaceStateChange);
        var documents = Documents.SetItems(updates);

        return Update(ProjectDifference.ProjectWorkspaceStateChanged, HostProject, projectWorkspaceState, documents, ImportsToRelatedDocuments);

        static KeyValuePair<string, DocumentState> WithProjectWorkspaceStateChange(KeyValuePair<string, DocumentState> pair)
        {
            return new(pair.Key, pair.Value.WithProjectWorkspaceStateChange());
        }
    }

    private ProjectState Update(
        ProjectDifference difference,
        HostProject hostProject,
        ProjectWorkspaceState projectWorkspaceState,
        ImmutableDictionary<string, DocumentState> documents,
        ImmutableDictionary<string, ImmutableArray<string>> importsToRelatedDocuments)
    {
        return new(this, Version.GetNewerVersion(), difference, hostProject, projectWorkspaceState, documents, importsToRelatedDocuments);
    }

    private static ImmutableDictionary<string, ImmutableArray<string>> AddToImportsToRelatedDocuments(
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

    private static List<string> GetImportDocumentTargetPaths(string targetPath, string fileKind, RazorProjectEngine projectEngine)
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
