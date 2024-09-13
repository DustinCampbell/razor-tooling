// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using static Microsoft.CodeAnalysis.Razor.ProjectSystem.DocumentState;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal static class IDocumentSnapshotExtensions
{
    public static async Task<TagHelperDescriptor?> TryGetTagHelperDescriptorAsync(this IDocumentSnapshot documentSnapshot, CancellationToken cancellationToken)
    {
        // No point doing anything if its not a component
        if (documentSnapshot.FileKind != FileKinds.Component)
        {
            return null;
        }

        var razorCodeDocument = await documentSnapshot.GetGeneratedOutputAsync().ConfigureAwait(false);
        if (razorCodeDocument is null)
        {
            return null;
        }

        var project = documentSnapshot.Project;

        // If we got this far, we can check for tag helpers
        var tagHelpers = await project.GetTagHelpersAsync(cancellationToken).ConfigureAwait(false);
        foreach (var tagHelper in tagHelpers)
        {
            // Check the typename and namespace match
            if (documentSnapshot.IsPathCandidateForComponent(tagHelper.GetTypeNameIdentifier().AsMemory()) &&
                razorCodeDocument.ComponentNamespaceMatches(tagHelper.GetTypeNamespace()))
            {
                return tagHelper;
            }
        }

        return null;
    }

    public static bool IsPathCandidateForComponent(this IDocumentSnapshot documentSnapshot, ReadOnlyMemory<char> path)
    {
        if (documentSnapshot.FileKind != FileKinds.Component)
        {
            return false;
        }

        var fileName = Path.GetFileNameWithoutExtension(documentSnapshot.FilePath);
        return fileName.AsSpan().Equals(path.Span, FilePathComparison.Instance);
    }

    public static Task<RazorCodeDocument> GetGeneratedOutputAsync(this IDocumentSnapshot documentSnapshot)
    {
        return documentSnapshot.GetGeneratedOutputAsync(forceDesignTimeGeneratedOutput: false);
    }

    public static async Task<ImmutableArray<ImportItem>> GetImportsAsync(this IDocumentSnapshot document, RazorProjectEngine projectEngine)
    {
        var imports = GetImportsCore(document.Project, projectEngine, document.FilePath.AssumeNotNull(), document.FileKind.AssumeNotNull());
        using var result = new PooledArrayBuilder<ImportItem>(imports.Length);

        foreach (var snapshot in imports)
        {
            var versionStamp = await snapshot.GetTextVersionAsync().ConfigureAwait(false);
            result.Add(new ImportItem(snapshot.FilePath, versionStamp, snapshot));
        }

        return result.DrainToImmutable();
    }

    private static ImmutableArray<IDocumentSnapshot> GetImportsCore(IProjectSnapshot project, RazorProjectEngine projectEngine, string filePath, string fileKind)
    {
        var projectItem = projectEngine.FileSystem.GetItem(filePath, fileKind);

        using var _1 = ListPool<RazorProjectItem>.GetPooledObject(out var importItems);

        foreach (var feature in projectEngine.ProjectFeatures.OfType<IImportProjectFeature>())
        {
            if (feature.GetImports(projectItem) is { } featureImports)
            {
                importItems.AddRange(featureImports);
            }
        }

        if (importItems.Count == 0)
        {
            return [];
        }

        using var _2 = ArrayBuilderPool<IDocumentSnapshot>.GetPooledObject(out var imports);

        foreach (var item in importItems)
        {
            if (item is NotFoundProjectItem)
            {
                continue;
            }

            if (item.PhysicalPath is null)
            {
                // This is a default import.
                var defaultImport = new ImportDocumentSnapshot(project, item);
                imports.Add(defaultImport);
            }
            else if (project.TryGetDocument(item.PhysicalPath, out var import))
            {
                imports.Add(import);
            }
        }

        return imports.ToImmutable();
    }

    internal static async Task<RazorCodeDocument> GenerateCodeDocumentAsync(this IDocumentSnapshot document,
        RazorProjectEngine projectEngine,
        ImmutableArray<ImportItem> imports,
        ImmutableArray<TagHelperDescriptor> tagHelpers,
        bool forceRuntimeCodeGeneration)
    {
        // OK we have to generate the code.
        using var importSources = new PooledArrayBuilder<RazorSourceDocument>(imports.Length);
        foreach (var item in imports)
        {
            var importProjectItem = item.FilePath is null ? null : projectEngine.FileSystem.GetItem(item.FilePath, item.FileKind);
            var sourceDocument = await GetRazorSourceDocumentAsync(item.Document, importProjectItem).ConfigureAwait(false);
            importSources.Add(sourceDocument);
        }

        var projectItem = document.FilePath is null ? null : projectEngine.FileSystem.GetItem(document.FilePath, document.FileKind);
        var documentSource = await GetRazorSourceDocumentAsync(document, projectItem).ConfigureAwait(false);

        if (forceRuntimeCodeGeneration)
        {
            return projectEngine.Process(documentSource, fileKind: document.FileKind, importSources.DrainToImmutable(), tagHelpers);
        }

        return projectEngine.ProcessDesignTime(documentSource, fileKind: document.FileKind, importSources.DrainToImmutable(), tagHelpers);
    }

    private static async Task<RazorSourceDocument> GetRazorSourceDocumentAsync(IDocumentSnapshot document, RazorProjectItem? projectItem)
    {
        var sourceText = await document.GetTextAsync().ConfigureAwait(false);
        return RazorSourceDocument.Create(sourceText, RazorSourceDocumentProperties.Create(document.FilePath, projectItem?.RelativePhysicalPath));
    }
}
