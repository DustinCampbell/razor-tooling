// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.ProjectSystem;

internal static class Extensions
{
    public static ValueTask<SourceText> GetTextAsync(this IDocumentSnapshot document, CancellationToken cancellationToken)
    {
        return document.TryGetSource(out var result)
            ? new(result.Text)
            : new(GetTextCoreAsync(document, cancellationToken));

        static async Task<SourceText> GetTextCoreAsync(IDocumentSnapshot document, CancellationToken cancellationToken)
        {
            var source = await document.GetSourceAsync(cancellationToken).ConfigureAwait(false);

            return source.Text;
        }
    }

    public static bool TryGetText(this IDocumentSnapshot document, [NotNullWhen(true)] out SourceText? result)
    {
        if (document.TryGetSource(out var source))
        {
            result = source.Text;
            return true;
        }

        result = null;
        return false;
    }
}
