// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;

[Export(typeof(IDocumentMappingService)), Shared]
[method: ImportingConstructor]
internal sealed class RemoteDocumentMappingService(
    IFilePathService filePathService,
    ILoggerFactory loggerFactory)
    : AbstractDocumentMappingService(loggerFactory.GetOrCreateLogger<RemoteDocumentMappingService>())
{
    private readonly IFilePathService _filePathService = filePathService;

    public async Task<(Uri MappedDocumentUri, LinePositionSpan MappedRange)> MapToHostDocumentUriAndRangeAsync(
        RemoteRazorDocument originDocument,
        Uri generatedDocumentUri,
        LinePositionSpan generatedDocumentRange,
        CancellationToken cancellationToken)
    {
        var razorDocumentUri = _filePathService.GetRazorDocumentUri(generatedDocumentUri);

        // For Html we just map the Uri, the range will be the same
        if (_filePathService.IsVirtualHtmlFile(generatedDocumentUri))
        {
            return (razorDocumentUri, generatedDocumentRange);
        }

        // We only map from C# files
        if (!_filePathService.IsVirtualCSharpFile(generatedDocumentUri))
        {
            return (generatedDocumentUri, generatedDocumentRange);
        }

        var solution = originDocument.Project.Solution;
        if (!solution.TryGetDocument(razorDocumentUri, out var document))
        {
            return (generatedDocumentUri, generatedDocumentRange);
        }

        var codeDocument = await document
            .GetGeneratedOutputAsync(cancellationToken)
            .ConfigureAwait(false);

        if (codeDocument is null)
        {
            return (generatedDocumentUri, generatedDocumentRange);
        }

        if (!codeDocument.TryGetGeneratedDocument(generatedDocumentUri, _filePathService, out var generatedDocument))
        {
            return Assumed.Unreachable<(Uri, LinePositionSpan)>();
        }

        if (TryMapToHostDocumentRange(generatedDocument, generatedDocumentRange, MappingBehavior.Strict, out var mappedRange))
        {
            return (razorDocumentUri, mappedRange);
        }

        return (generatedDocumentUri, generatedDocumentRange);
    }
}
