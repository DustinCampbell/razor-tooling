// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal readonly struct RazorCodeDocumentCompiler(RazorProjectEngine projectEngine)
{
    public async Task<RazorCodeDocument> CompileCodeDocumentAsync(
        IDocumentSnapshot document,
        ImmutableArray<VersionStamp> otherInputVersions,
        CancellationToken cancellationToken)
    {
        // First, compute the most recent version of the document and other inputs.
        var documentVersion = await document.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);

        var inputVersion = documentVersion;
        foreach (var version in otherInputVersions)
        {
            inputVersion = inputVersion.GetNewerVersion(version);
        }

        // Next, gather up import documents.
        using var _ = ListPool<IDocumentSnapshot>.GetPooledObject(out var importDocuments);
        CollectImportDocuments(importDocuments, document, projectEngine);

        // The import documents are also inputs, so we need to update the version if any imports are newer.
        foreach (var importDocument in importDocuments)
        {
            var importDocumentVersion = await importDocument.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
            inputVersion = inputVersion.GetNewerVersion(importDocumentVersion);
        }

        var importSources = await ConvertToSourceDocumentsAsync(importDocuments, projectEngine, cancellationToken).ConfigureAwait(false);

        var tagHelpers = await document.Project.GetTagHelpersAsync(cancellationToken).ConfigureAwait(false);
        var forceRuntimeCodeGeneration = document.Project.Configuration.LanguageServerFlags?.ForceRuntimeCodeGeneration ?? false;

        var source = await document.ToRazorSourceDocumentAsync(projectEngine, cancellationToken).ConfigureAwait(false);

        if (forceRuntimeCodeGeneration)
        {
            return projectEngine.Process(source, document.FileKind, importSources, tagHelpers);
        }

        return projectEngine.ProcessDesignTime(source, document.FileKind, importSources, tagHelpers);
    }

    private static void CollectImportDocuments(
        List<IDocumentSnapshot> importDocuments,
        IDocumentSnapshot document,
        RazorProjectEngine projectEngine)
    {
        var filePath = document.FilePath.AssumeNotNull();
        var fileKind = document.FileKind.AssumeNotNull();
        var projectItem = projectEngine.FileSystem.GetItem(filePath, fileKind);

        using var importProjectItems = new PooledArrayBuilder<RazorProjectItem>();

        foreach (var projectFeature in projectEngine.ProjectFeatures)
        {
            if (projectFeature is not IImportProjectFeature importProjectFeature ||
                importProjectFeature.GetImports(projectItem) is not { } importProjectItemList)
            {
                continue;
            }

            foreach (var importProjectItem in importProjectItemList.AsEnumerable())
            {
                if (importProjectItem is NotFoundProjectItem)
                {
                    continue;
                }

                importProjectItems.Add(importProjectItem);
            }
        }

        if (importProjectItems.Count == 0)
        {
            return;
        }

        var project = document.Project;

        foreach (var importProjectItem in importProjectItems)
        {
            var physicalPath = importProjectItem.PhysicalPath;

            if (physicalPath is null)
            {
                // This is a default import.
                var importDocument = new ImportDocumentSnapshot(project, importProjectItem);
                importDocuments.Add(importDocument);
            }
            else if (project.TryGetDocument(physicalPath, out var importDocument))
            {
                importDocuments.Add(importDocument);
            }
        }
    }

    private static async ValueTask<ImmutableArray<RazorSourceDocument>> ConvertToSourceDocumentsAsync(
        List<IDocumentSnapshot> importDocuments,
        RazorProjectEngine projectEngine,
        CancellationToken cancellationToken)
    {
        using var sourceDocuments = new PooledArrayBuilder<RazorSourceDocument>(capacity: importDocuments.Count);

        foreach (var importDocument in importDocuments)
        {
            var sourceDocument = await importDocument.ToRazorSourceDocumentAsync(projectEngine, cancellationToken).ConfigureAwait(false);
            sourceDocuments.Add(sourceDocument);
        }

        return sourceDocuments.DrainToImmutable();
    }
}
