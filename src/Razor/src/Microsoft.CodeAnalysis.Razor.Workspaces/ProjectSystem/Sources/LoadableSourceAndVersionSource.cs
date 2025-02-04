// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem.Sources;

internal sealed class LoadableSourceAndVersionSource(
    TextLoader textLoader,
    RazorSourceDocumentProperties properties)
    : ISourceAndVersionSource
{
    public TextLoader? TextLoader => textLoader;

    private static readonly LoadTextOptions s_loadTextOptions = new(SourceHashAlgorithm.Sha256);

    private readonly AsyncLazy<SourceAndVersion> _lazy = AsyncLazy.Create(LoadSourceAsync, (textLoader, properties));

    private static async Task<SourceAndVersion> LoadSourceAsync(
        (TextLoader textLoader, RazorSourceDocumentProperties properties) arg,
        CancellationToken cancellationToken)
    {
        var textAndVersion = await arg.textLoader.LoadTextAndVersionAsync(s_loadTextOptions, cancellationToken).ConfigureAwait(false);

        return new(
            RazorSourceDocument.Create(textAndVersion.Text, arg.properties),
            textAndVersion.Version);
    }

    public ValueTask<SourceAndVersion> GetValueAsync(CancellationToken cancellationToken)
        => new(_lazy.GetValueAsync(cancellationToken));

    public bool TryGetValue([NotNullWhen(true)] out SourceAndVersion? result)
        => _lazy.TryGetValue(out result);
}
