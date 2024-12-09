// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal static class IDocumentSnapshotExtensions
{
    public static async Task<TagHelperDescriptor?> TryGetTagHelperDescriptorAsync(
        this IDocumentSnapshot documentSnapshot,
        CancellationToken cancellationToken)
    {
        // No point doing anything if its not a component
        if (documentSnapshot.FileKind != FileKinds.Component)
        {
            return null;
        }

        var razorCodeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
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

    public static async Task<RazorCodeDocument> GenerateCodeDocumentAsync(
        this IDocumentSnapshot document,
        RazorProjectEngine projectEngine,
        bool forceRuntimeCodeGeneration,
        CancellationToken cancellationToken)
    {
        var importItems = await document.GetImportItemsAsync(projectEngine, cancellationToken).ConfigureAwait(false);

        return await document.GenerateCodeDocumentAsync(
            projectEngine, importItems, forceRuntimeCodeGeneration, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<RazorCodeDocument> GenerateCodeDocumentAsync(
        this IDocumentSnapshot document,
        RazorProjectEngine projectEngine,
        ImmutableArray<ImportItem> imports,
        bool forceRuntimeCodeGeneration,
        CancellationToken cancellationToken)
    {
        var importSourceDocuments = imports.SelectAsArray(static i => i.CreateSourceDocument());
        var tagHelpers = await document.Project.GetTagHelpersAsync(cancellationToken).ConfigureAwait(false);
        var sourceDocument = await document.CreateSourceDocumentAsync(projectEngine.FileSystem, cancellationToken).ConfigureAwait(false);

        return forceRuntimeCodeGeneration
            ? projectEngine.Process(sourceDocument, document.FileKind, importSourceDocuments, tagHelpers)
            : projectEngine.ProcessDesignTime(sourceDocument, document.FileKind, importSourceDocuments, tagHelpers);
    }

    public static ValueTask<RazorCodeDocument> GetGeneratedOutputAsync(this IDocumentSnapshot documentSnapshot, CancellationToken cancellationToken)
        => documentSnapshot.GetGeneratedOutputAsync(forceDesignTimeGeneratedOutput: false, cancellationToken);

    public static ValueTask<RazorSourceDocument> CreateSourceDocumentAsync(
        this IDocumentSnapshot document,
        RazorProjectFileSystem fileSystem,
        CancellationToken cancellationToken)
    {
        var projectItem = fileSystem.GetProjectItem(document);

        return document.TryGetText(out var text)
            ? new(CreateSourceDocument(document.FilePath, projectItem.RelativePhysicalPath, text))
            : new(CreateSourceDocumentCoreAsync(document, projectItem.RelativePhysicalPath, cancellationToken));

        static async Task<RazorSourceDocument> CreateSourceDocumentCoreAsync(
            IDocumentSnapshot document, string? relativePhysicalPath, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return CreateSourceDocument(document.FilePath, relativePhysicalPath, text);
        }
    }

    private static RazorSourceDocument CreateSourceDocument(string filePath, string? relativePath, SourceText text)
        => RazorSourceDocument.Create(text, RazorSourceDocumentProperties.Create(filePath, relativePath));
}
