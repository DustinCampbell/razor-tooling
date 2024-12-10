// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal static class RazorProjectEngineExtensions
{
    public static RazorProjectItem GetProjectItem(this RazorProjectEngine projectEngine, IDocumentSnapshot document)
        => projectEngine.FileSystem.GetProjectItem(document);

    public static RazorProjectItem GetProjectItem(this RazorProjectEngine projectEngine, HostDocument hostDocument)
        => projectEngine.FileSystem.GetProjectItem(hostDocument);

    public static async Task<ImmutableArray<ImportItem>> GetImportItemsAsync(
        this IDocumentSnapshot document,
        RazorProjectEngine projectEngine,
        CancellationToken cancellationToken)
    {
        var projectItem = projectEngine.GetProjectItem(document);

        using var importProjectItems = new PooledArrayBuilder<RazorProjectItem>();
        projectEngine.CollectImportProjectItems(projectItem, ref importProjectItems.AsRef());

        if (importProjectItems.Count == 0)
        {
            return [];
        }

        var project = document.Project;

        using var importItems = new PooledArrayBuilder<ImportItem>(capacity: importProjectItems.Count);

        foreach (var importProjectItem in importProjectItems)
        {
            if (importProjectItem is NotFoundProjectItem)
            {
                continue;
            }

            if (importProjectItem.PhysicalPath is null)
            {
                // This is a default import.
                using var stream = importProjectItem.Read();
                var text = SourceText.From(stream);

                importItems.Add(new(importProjectItem, TextAndVersion.Create(text, version: default)));

            }
            else if (project.TryGetDocument(importProjectItem.PhysicalPath, out var importDocument))
            {
                var text = await importDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var versionStamp = await importDocument.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);

                importItems.Add(new(importProjectItem, TextAndVersion.Create(text, versionStamp)));
            }
        }

        return importItems.DrainToImmutable();
    }

    public static void CollectImportProjectItems(
        this RazorProjectEngine projectEngine,
        RazorProjectItem projectItem,
        ref PooledArrayBuilder<RazorProjectItem> importProjectItems)
    {
        foreach (var projectFeature in projectEngine.ProjectFeatures)
        {
            if (projectFeature is IImportProjectFeature importProjectFeature)
            {
                importProjectItems.AddRange(importProjectFeature.GetImports(projectItem));
            }
        }
    }
}
