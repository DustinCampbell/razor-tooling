// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.LanguageServer;

internal class RazorDocumentTextLoader(IRazorDocument document) : TextLoader
{
    private readonly IRazorDocument _document = document;

    public override async Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
    {
        var sourceText = await _document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var textAndVersion = TextAndVersion.Create(sourceText, VersionStamp.Default);

        return textAndVersion;
    }
}
