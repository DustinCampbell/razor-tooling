// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Razor.Logging;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem.Sources;

internal sealed class GeneratedOutputSource(ILogger logger)
{
    private static int s_totalCompiles;
    private static readonly Dictionary<string, StrongBox<int>> s_targetPathToCompiles = new(FilePathComparer.Instance);

    private readonly SemaphoreSlim _gate = new(initialCount: 1);
    private readonly ILogger _logger = logger;

    private GeneratedOutput? _output;

    public bool TryGetValue([NotNullWhen(true)] out GeneratedOutput? result)
    {
        result = _output;
        return result is not null;
    }

    public async ValueTask<GeneratedOutput> GetValueAsync(DocumentSnapshot document, CancellationToken cancellationToken)
    {
        if (TryGetValue(out var result))
        {
            return result;
        }

        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (TryGetValue(out result))
            {
                return result;
            }

            s_totalCompiles++;

            var compileCount = s_targetPathToCompiles.GetOrAdd(document.FilePath, _ => new());
            compileCount.Value++;

            _logger.LogInformation($"Compile({compileCount.Value}) {document.Project.DisplayName}:{document.TargetPath}");
            _logger.LogInformation($"{s_totalCompiles} total compile(s).");

            var project = document.Project;
            var projectEngine = project.ProjectEngine;
            var compilerOptions = project.CompilerOptions;

            var source = await document.GetSourceAsync(cancellationToken).ConfigureAwait(false);
            var (importSources, importsVersion) = await GetImportSourcesAndVersionAsync(document, projectEngine, cancellationToken).ConfigureAwait(false);
            var tagHelpers = await project.GetTagHelpersAsync(cancellationToken).ConfigureAwait(false);

            var codeDocument = CompilationHelpers.GenerateCodeDocument(
                source, document.FileKind, importSources, tagHelpers, projectEngine, compilerOptions, cancellationToken);

            _output = new(codeDocument, importsVersion);

            return _output;
        }
    }

    private static async Task<(ImmutableArray<RazorSourceDocument>, VersionStamp)> GetImportSourcesAndVersionAsync(
        DocumentSnapshot document,
        RazorProjectEngine projectEngine,
        CancellationToken cancellationToken)
    {
        var projectItem = projectEngine.FileSystem.GetItem(document.FilePath, document.FileKind);

        using var importProjectItems = new PooledArrayBuilder<RazorProjectItem>();
        projectEngine.CollectImports(projectItem, ref importProjectItems.AsRef());

        if (importProjectItems.Count == 0)
        {
            return ([], default);
        }

        var project = document.Project;

        using var importSources = new PooledArrayBuilder<RazorSourceDocument>(capacity: importProjectItems.Count);

        var version = VersionStamp.Default;

        foreach (var importProjectItem in importProjectItems)
        {
            if (importProjectItem is NotFoundProjectItem)
            {
                continue;
            }

            if (importProjectItem is DefaultImportProjectItem)
            {
                var importSource = importProjectItem.GetSource()
                    .AssumeNotNull($"Encountered a default import with a missing {nameof(RazorSourceDocument)}: {importProjectItem.FilePath}.");

                // Note: Because default imports never change and don't manifest on disk,
                // we don't need to track their version.

                importSources.Add(importSource);
            }
            else if (project.TryGetDocument(importProjectItem.PhysicalPath, out var importDocument))
            {
                var (importSource, importVersion) = await importDocument.GetSourceAndVersionAsync(cancellationToken).ConfigureAwait(false);

                version = version.GetNewerVersion(importVersion);

                importSources.Add(importSource);
            }
        }

        return (importSources.DrainToImmutable(), version);
    }
}
