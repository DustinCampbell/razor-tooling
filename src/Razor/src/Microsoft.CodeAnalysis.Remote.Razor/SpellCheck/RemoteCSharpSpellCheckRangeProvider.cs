// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.SpellCheck;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.SpellCheck;

[Export(typeof(ICSharpSpellCheckRangeProvider)), Shared]
[method: ImportingConstructor]
internal sealed class RemoteCSharpSpellCheckRangeProvider() : ICSharpSpellCheckRangeProvider
{
    public async Task<ImmutableArray<SpellCheckRange>> GetCSharpSpellCheckRangesAsync(DocumentContext documentContext, CancellationToken cancellationToken)
    {
        // We have a razor document, lets find the generated C# document
        var document = documentContext.Document.ToRemoteRazorDocument();
        var generatedDocument = await document.GetGeneratedDocumentAsync(cancellationToken).ConfigureAwait(false);

        var csharpRanges = await ExternalAccess.Razor.Cohost.Handlers.SpellCheck.GetSpellCheckSpansAsync(generatedDocument, cancellationToken).ConfigureAwait(false);

        return csharpRanges.SelectAsArray(static r => new SpellCheckRange((int)r.Kind, r.StartIndex, r.Length));
    }
}
