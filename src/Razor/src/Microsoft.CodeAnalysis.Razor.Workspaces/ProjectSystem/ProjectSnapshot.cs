// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal class ProjectSnapshot(ProjectState state) : IProjectSnapshot
{
    private readonly ProjectState _state = state;

    private readonly object _lock = new();
    private readonly Dictionary<string, DocumentSnapshot> _documents = new(FilePathNormalizingComparer.Instance);

    public ProjectKey Key => _state.HostProject.Key;

    public RazorConfiguration Configuration => HostProject.Configuration;

    public IEnumerable<string> DocumentFilePaths => _state.Documents.Keys;

    public int DocumentCount => _state.Documents.Count;

    public string FilePath => _state.HostProject.FilePath;

    public string IntermediateOutputPath => _state.HostProject.IntermediateOutputPath;

    public string? RootNamespace => _state.HostProject.RootNamespace;

    public string DisplayName => _state.HostProject.DisplayName;

    public LanguageVersion CSharpLanguageVersion => _state.CSharpLanguageVersion;

    public HostProject HostProject => _state.HostProject;

    public virtual VersionStamp Version => _state.Version;

    public VersionStamp ConfigurationVersion => _state.ConfigurationVersion;

    public VersionStamp DocumentCollectionVersion => _state.DocumentCollectionVersion;

    public VersionStamp ProjectWorkspaceStateVersion => _state.ProjectWorkspaceStateVersion;

    public ValueTask<ImmutableArray<TagHelperDescriptor>> GetTagHelpersAsync(CancellationToken cancellationToken) => new(_state.TagHelpers);

    public ProjectWorkspaceState ProjectWorkspaceState => _state.ProjectWorkspaceState;

    public virtual IDocumentSnapshot? GetDocument(string filePath)
    {
        lock (_lock)
        {
            if (!_documents.TryGetValue(filePath, out var result) &&
                _state.Documents.TryGetValue(filePath, out var state))
            {
                result = new DocumentSnapshot(this, state);
                _documents.Add(filePath, result);
            }

            return result;
        }
    }

    public bool TryGetDocument(string filePath, [NotNullWhen(true)] out IDocumentSnapshot? document)
    {
        document = GetDocument(filePath);
        return document is not null;
    }

    /// <summary>
    /// If the provided document is an import document, gets the other documents in the project
    /// that include directives specified by the provided document. Otherwise returns an empty
    /// list.
    /// </summary>
    public ImmutableArray<IDocumentSnapshot> GetRelatedDocuments(IDocumentSnapshot document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var targetPath = document.TargetPath.AssumeNotNull();

        if (!_state.ImportsToRelatedDocuments.TryGetValue(targetPath, out var relatedDocuments))
        {
            return ImmutableArray<IDocumentSnapshot>.Empty;
        }

        lock (_lock)
        {
            using var _ = ArrayBuilderPool<IDocumentSnapshot>.GetPooledObject(out var builder);

            foreach (var relatedDocumentFilePath in relatedDocuments)
            {
                if (GetDocument(relatedDocumentFilePath) is { } relatedDocument)
                {
                    builder.Add(relatedDocument);
                }
            }

            return builder.ToImmutableArray();
        }
    }

    public virtual RazorProjectEngine GetProjectEngine()
    {
        return _state.ProjectEngine;
    }
}
