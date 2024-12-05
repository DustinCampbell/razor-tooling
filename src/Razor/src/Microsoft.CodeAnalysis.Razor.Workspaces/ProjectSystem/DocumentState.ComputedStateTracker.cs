// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Threading;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal partial class DocumentState
{
    private sealed class ComputedStateTracker
    {
        private readonly SemaphoreSlim _gate = new(initialCount: 1);
        private WeakReference<OutputAndVersion>? _weakOutputAndVersion;

        public bool TryGetGeneratedOutputAndVersion([NotNullWhen(true)] out OutputAndVersion? result)
        {
            using (_gate.DisposableWait())
            {
                if (_weakOutputAndVersion is { } weakResult && weakResult.TryGetTarget(out result))
                {
                    return true;
                }
            }

            result = null;
            return false;
        }

        public async Task<OutputAndVersion> GetGeneratedOutputAndVersionAsync(DocumentSnapshot document, CancellationToken cancellationToken)
        {
            var project = document.Project;
            var projectEngine = project.GetProjectEngine();
            var importItems = await GetImportItemsAsync(document, projectEngine, cancellationToken).ConfigureAwait(false);
            var version = await GetLatestVersionAsync(document, importItems, cancellationToken).ConfigureAwait(false);

            using var _ = await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false);

            var weakOutputAndVersion = _weakOutputAndVersion;

            // Do we already have cached output with the same version? If so, there's no reason to re-generate it.
            if (weakOutputAndVersion is not null &&
                weakOutputAndVersion.TryGetTarget(out var result) &&
                result.Version == version)
            {
                return result;
            }

            var forceRuntimeCodeGeneration = project.LanguageServerFeatureOptions.ForceRuntimeCodeGeneration;
            var codeDocument = await GenerateCodeDocumentAsync(document, projectEngine, importItems, forceRuntimeCodeGeneration, cancellationToken).ConfigureAwait(false);

            result = new OutputAndVersion(codeDocument, version);

            if (weakOutputAndVersion is not null)
            {
                weakOutputAndVersion.SetTarget(result);
            }
            else
            {
                _weakOutputAndVersion = new(result);
            }

            return result;
        }

        private static async ValueTask<VersionStamp> GetLatestVersionAsync(DocumentSnapshot document, ImmutableArray<ImportItem> importItems, CancellationToken cancellationToken)
        {
            // We only need to produce the generated code if any of our inputs is newer than the
            // previously cached output.
            //
            // First find the versions that are the inputs:
            // - The project + computed state
            // - The imports
            // - This document
            //
            // All of these things are cached, so no work is wasted if we do need to generate the code.

            var version = await document.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);

            version = version.GetNewerVersion(document.Project.GetLatestVersion());

            foreach (var import in importItems)
            {
                version = version.GetNewerVersion(import.Version);
            }

            return version;
        }
    }
}
