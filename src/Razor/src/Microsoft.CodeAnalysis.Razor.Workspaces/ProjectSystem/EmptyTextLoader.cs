// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal sealed class EmptyTextLoader : TextLoader
{
    public static TextLoader Instance { get; } = new EmptyTextLoader();

    private static readonly SourceText s_emptyText = SourceText.From(string.Empty, Encoding.UTF8, SourceHashAlgorithm.Sha256);
    private static readonly TextAndVersion s_textAndVersion = TextAndVersion.Create(s_emptyText, version: default);
    private static readonly Task<TextAndVersion> s_loadTextAndVersionResult = Task.FromResult(s_textAndVersion);

    private EmptyTextLoader()
    {
    }

    public override Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
        => s_loadTextAndVersionResult;
}
