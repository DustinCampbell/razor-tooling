// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal partial class DocumentState
{
    internal abstract class ImportItem(RazorProjectItem projectItem)
    {
        private readonly RazorProjectItem _projectItem = projectItem;
        private RazorSourceDocument? _sourceDocument;

        public bool IsDefault => _projectItem.PhysicalPath is null;

        public string? FilePath { get; } = projectItem.PhysicalPath;
        public string? RelativePath { get; } = projectItem.RelativePhysicalPath;

        public static ImportItem CreateDefaultImport(RazorProjectItem projectItem)
        {
            using var stream = projectItem.Read();
            var text = SourceText.From(stream);

            return new DefaultImportItem(text, projectItem);
        }

        public static ImportItem CreateDocumentImport(IDocumentSnapshot document, RazorProjectItem projectItem)
            => new DocumentImportItem(document, projectItem);

        protected abstract ValueTask<RazorSourceDocument> CreateSourceDocumentAsync(RazorSourceDocumentProperties properties, CancellationToken cancellationToken);

        public ValueTask<RazorSourceDocument> GetSourceDocumentAsync(CancellationToken cancellationToken)
        {
            return _sourceDocument is { } sourceDocument
                ? new(sourceDocument)
                : new(GetRazorSourceDocumentCoreAsync(cancellationToken));

            async Task<RazorSourceDocument> GetRazorSourceDocumentCoreAsync(CancellationToken cancellationToken)
            {
                var properties = RazorSourceDocumentProperties.Create(_projectItem.PhysicalPath, _projectItem.RelativePhysicalPath);
                var sourceDocument = await CreateSourceDocumentAsync(properties, cancellationToken).ConfigureAwait(false);

                return InterlockedOperations.Initialize(ref _sourceDocument, sourceDocument);
            }
        }

        public abstract ValueTask<VersionStamp> GetVersionAsync(CancellationToken cancellationToken);

        private sealed class DefaultImportItem(SourceText text, RazorProjectItem projectItem) : ImportItem(projectItem)
        {
            private readonly SourceText _text = text;

            protected override ValueTask<RazorSourceDocument> CreateSourceDocumentAsync(RazorSourceDocumentProperties properties, CancellationToken cancellationToken)
                => new(RazorSourceDocument.Create(_text, properties));

            public override ValueTask<VersionStamp> GetVersionAsync(CancellationToken cancellationToken)
                => new(VersionStamp.Default);
        }

        private sealed class DocumentImportItem(IDocumentSnapshot document, RazorProjectItem projectItem) : ImportItem(projectItem)
        {
            private readonly IDocumentSnapshot _document = document;

            protected override async ValueTask<RazorSourceDocument> CreateSourceDocumentAsync(RazorSourceDocumentProperties properties, CancellationToken cancellationToken)
            {
                var text = await _document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                return RazorSourceDocument.Create(text, properties);
            }

            public override ValueTask<VersionStamp> GetVersionAsync(CancellationToken cancellationToken)
                => _document.GetTextVersionAsync(cancellationToken);
        }
    }
}
