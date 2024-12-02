// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#if !NET
using System.Collections.Generic;
#endif

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.NET.Sdk.Razor.SourceGenerators;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

// Internal tracker for DefaultProjectSnapshot
internal partial class ProjectState
{
    private const ProjectDifference ClearConfigurationVersionMask = ProjectDifference.ConfigurationChanged;

    private const ProjectDifference ClearProjectWorkspaceStateVersionMask =
        ProjectDifference.ConfigurationChanged |
        ProjectDifference.ProjectWorkspaceStateChanged;

    private const ProjectDifference ClearDocumentCollectionVersionMask =
        ProjectDifference.ConfigurationChanged |
        ProjectDifference.DocumentAdded |
        ProjectDifference.DocumentRemoved;

    private static readonly ImmutableDictionary<string, ImmutableArray<string>> s_emptyImportsToRelatedDocuments = ImmutableDictionary.Create<string, ImmutableArray<string>>(FilePathNormalizingComparer.Instance);

    private readonly object _gate = new();

    private readonly IProjectEngineFactoryProvider _projectEngineFactoryProvider;
    private readonly LanguageServerFeatureOptions _languageServerFeatureOptions;
    private RazorProjectEngine? _projectEngine;

    public static ProjectState Create(
        IProjectEngineFactoryProvider projectEngineFactoryProvider,
        LanguageServerFeatureOptions languageServerFeatureOptions,
        HostProject hostProject,
        ProjectWorkspaceState projectWorkspaceState)
    {
        return new ProjectState(projectEngineFactoryProvider, languageServerFeatureOptions, hostProject, projectWorkspaceState);
    }

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
        DocumentStates = ImmutableDictionary.Create<string, DocumentState>(FilePathNormalizingComparer.Instance);
        ImportsToRelatedDocuments = s_emptyImportsToRelatedDocuments;
        Version = VersionStamp.Create();
        ProjectWorkspaceStateVersion = Version;
        DocumentCollectionVersion = Version;
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
        DocumentStates = documents;
        ImportsToRelatedDocuments = importsToRelatedDocuments;

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

    public ImmutableDictionary<string, DocumentState> DocumentStates { get; internal set; }
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
            lock (_gate)
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

    /// <summary>
    /// Gets the version of this project based on the project workspace state, NOT INCLUDING content
    /// changes. The computed state is guaranteed to change when the configuration or tag helpers
    /// change.
    /// </summary>
    public VersionStamp ProjectWorkspaceStateVersion { get; }

    public VersionStamp ConfigurationVersion { get; }

    public ProjectState AddDocument(HostDocument hostDocument, TextLoader textLoader)
    {
        ArgHelper.ThrowIfNull(hostDocument);
        ArgHelper.ThrowIfNull(textLoader);

        // Ignore attempts to 'add' a document with different data, we only
        // care about one, so it might as well be the one we have.
        if (DocumentStates.ContainsKey(hostDocument.FilePath))
        {
            return this;
        }

        var documentState = DocumentState.Create(hostDocument, version: 1, textLoader);
        var newDocuments = DocumentStates.Add(hostDocument.FilePath, documentState);

        // Compute the effect on the import map
        var importsToRelatedDocuments = AddToImportsToRelatedDocuments(hostDocument, ImportsToRelatedDocuments);

        // Now check if the updated document is an import - it's important this this happens after
        // updating the imports map.
        if (importsToRelatedDocuments.TryGetValue(hostDocument.TargetPath, out var relatedDocuments))
        {
            foreach (var relatedDocument in relatedDocuments)
            {
                newDocuments = newDocuments.SetItem(relatedDocument, newDocuments[relatedDocument].WithImportsChange());
            }
        }

        return new(this, ProjectDifference.DocumentAdded, HostProject, ProjectWorkspaceState, newDocuments, importsToRelatedDocuments);
    }

    public ProjectState RemoveDocument(string documentFilePath)
    {
        ArgHelper.ThrowIfNull(documentFilePath);

        if (!DocumentStates.TryGetValue(documentFilePath, out var documentState))
        {
            return this;
        }

        var documents = DocumentStates.Remove(documentFilePath);

        var hostDocument = documentState.HostDocument;

        // First check if the updated document is an import - it's important that this happens
        // before updating the imports map.
        if (ImportsToRelatedDocuments.TryGetValue(hostDocument.TargetPath, out var relatedDocuments))
        {
            foreach (var relatedDocument in relatedDocuments)
            {
                documents = documents.SetItem(relatedDocument, documents[relatedDocument].WithImportsChange());
            }
        }

        // Compute the effect on the import map
        var importsToRelatedDocuments = RemoveFromImportsToRelatedDocuments(hostDocument, ImportsToRelatedDocuments);

        return new(this, ProjectDifference.DocumentRemoved, HostProject, ProjectWorkspaceState, documents, importsToRelatedDocuments);
    }

    public ProjectState UpdateDocumentText(string documentFilePath, TextLoader textLoader)
    {
        ArgHelper.ThrowIfNull(documentFilePath);

        if (!DocumentStates.TryGetValue(documentFilePath, out var documentState))
        {
            return this;
        }

        var documents = DocumentStates.SetItem(documentFilePath, documentState.WithTextLoader(textLoader));

        if (ImportsToRelatedDocuments.TryGetValue(documentState.HostDocument.TargetPath, out var relatedDocuments))
        {
            foreach (var relatedDocument in relatedDocuments)
            {
                documents = documents.SetItem(relatedDocument, documents[relatedDocument].WithImportsChange());
            }
        }

        return new(this, ProjectDifference.DocumentChanged, HostProject, ProjectWorkspaceState, documents, ImportsToRelatedDocuments);
    }

    public ProjectState UpdateDocumentText(string documentFilePath, SourceText sourceText)
    {
        ArgHelper.ThrowIfNull(documentFilePath);

        if (!DocumentStates.TryGetValue(documentFilePath, out var documentState))
        {
            return this;
        }

        if (documentState.TryGetTextAndVersion(out var textAndVersion))
        {
            var olderText = textAndVersion.Text;
            var olderVersion = textAndVersion.Version;

            var newVersion = sourceText.ContentEquals(olderText)
                ? olderVersion
                : olderVersion.GetNewerVersion();

            return UpdateDocumentText(documentState, state => state.WithText(sourceText, newVersion));
        }

        return UpdateDocumentText(documentState, state => state.WithTextLoader(new UpdatedTextLoader(state, sourceText)));
    }

    private ProjectState UpdateDocumentText(DocumentState documentState, Func<DocumentState, DocumentState> documentStateUpdater)
    {
        var hostDocument = documentState.HostDocument;
        var documents = DocumentStates.SetItem(hostDocument.FilePath, documentStateUpdater(documentState));

        if (ImportsToRelatedDocuments.TryGetValue(hostDocument.TargetPath, out var relatedDocuments))
        {
            foreach (var relatedDocument in relatedDocuments)
            {
                documents = documents.SetItem(relatedDocument, documents[relatedDocument].WithImportsChange());
            }
        }

        return new(this, ProjectDifference.DocumentChanged, HostProject, ProjectWorkspaceState, documents, ImportsToRelatedDocuments);
    }

    public ProjectState WithHostProject(HostProject hostProject)
    {
        ArgHelper.ThrowIfNull(hostProject);

        if (HostProject.Configuration.Equals(hostProject.Configuration) &&
            HostProject.RootNamespace == hostProject.RootNamespace)
        {
            return this;
        }

        var documents = DocumentStates.ToImmutableDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.WithConfigurationChange(),
            FilePathNormalizingComparer.Instance);

        // If the host project has changed then we need to recompute the imports map
        var importsToRelatedDocuments = s_emptyImportsToRelatedDocuments;

        foreach (var (_, state) in documents)
        {
            importsToRelatedDocuments = AddToImportsToRelatedDocuments(state.HostDocument, importsToRelatedDocuments);
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

        var difference = ProjectDifference.ProjectWorkspaceStateChanged;
        var documents = DocumentStates.ToImmutableDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.WithProjectWorkspaceStateChange(),
            FilePathNormalizingComparer.Instance);

        return new(this, difference, HostProject, projectWorkspaceState, documents, ImportsToRelatedDocuments);
    }

    private ImmutableDictionary<string, ImmutableArray<string>> AddToImportsToRelatedDocuments(
        HostDocument hostDocument,
        ImmutableDictionary<string, ImmutableArray<string>> importsToRelatedDocuments)
    {
        var importTargetPaths = ProjectEngine.GetImportDocumentTargetPaths(hostDocument);
        var documentFilePath = hostDocument.FilePath;

        foreach (var importTargetPath in importTargetPaths)
        {
            var relatedDocuments = importsToRelatedDocuments.TryGetValue(importTargetPath, out var existingDocuments)
                ? existingDocuments.Add(documentFilePath)
                : [documentFilePath];

            importsToRelatedDocuments = importsToRelatedDocuments.SetItem(importTargetPath, relatedDocuments);
        }

        return importsToRelatedDocuments;
    }

    private ImmutableDictionary<string, ImmutableArray<string>> RemoveFromImportsToRelatedDocuments(
        HostDocument hostDocument,
        ImmutableDictionary<string, ImmutableArray<string>> importsToRelatedDocuments)
    {
        var importTargetPaths = ProjectEngine.GetImportDocumentTargetPaths(hostDocument);
        var documentFilePath = hostDocument.FilePath;

        foreach (var importTargetPath in importTargetPaths)
        {
            if (importsToRelatedDocuments.TryGetValue(importTargetPath, out var relatedDocuments))
            {
                relatedDocuments = relatedDocuments.Remove(documentFilePath);
                importsToRelatedDocuments = relatedDocuments.Length > 0
                    ? importsToRelatedDocuments.SetItem(importTargetPath, relatedDocuments)
                    : importsToRelatedDocuments.Remove(importTargetPath);
            }
        }

        return importsToRelatedDocuments;
    }

    private sealed class UpdatedTextLoader(DocumentState oldState, SourceText newSourceText) : TextLoader
    {
        public override async Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
        {
            var oldTextAndVersion = await oldState.GetTextAndVersionAsync(cancellationToken).ConfigureAwait(false);

            var newVersion = newSourceText.ContentEquals(oldTextAndVersion.Text)
                ? oldTextAndVersion.Version
                : oldTextAndVersion.Version.GetNewerVersion();

            return TextAndVersion.Create(newSourceText, newVersion);
        }
    }
}
