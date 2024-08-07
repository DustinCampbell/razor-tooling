// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal abstract class RazorTextLoader
{
    public static readonly RazorTextLoader Empty = Create(string.Empty, VersionStamp.Default);

    public static RazorTextLoader Create(TextLoader? textLoader)
        => textLoader is not null
            ? new WrappedRoslynLoader(textLoader)
            : Empty;

    public static RazorTextLoader Create(SourceText text, VersionStamp version)
        => new SimpleLoader(text, version);

    public static RazorTextLoader Create(string text, VersionStamp version)
        => Create(SourceText.From(text), version);

    public static RazorTextLoader Create(Func<CancellationToken, Task<TextAndVersion>> loader)
        => new DelegateLoader(loader);

    public abstract Task<TextAndVersion> LoadTextAndVersionAsync(CancellationToken cancellationToken);

    private sealed class SimpleLoader(SourceText text, VersionStamp version) : RazorTextLoader
    {
        private readonly Task<TextAndVersion> _task = Task.FromResult(TextAndVersion.Create(text, version));

        public override Task<TextAndVersion> LoadTextAndVersionAsync(CancellationToken cancellationToken)
            => _task;
    }

    private sealed class DelegateLoader(Func<CancellationToken, Task<TextAndVersion>> loader) : RazorTextLoader
    {
        public override Task<TextAndVersion> LoadTextAndVersionAsync(CancellationToken cancellationToken)
            => loader(cancellationToken);
    }

    private sealed class WrappedRoslynLoader(TextLoader textLoader) : RazorTextLoader
    {
        private static readonly LoadTextOptions s_loadTextOptions = new(SourceHashAlgorithm.Sha256);

        public override Task<TextAndVersion> LoadTextAndVersionAsync(CancellationToken cancellationToken)
            => textLoader.LoadTextAndVersionAsync(s_loadTextOptions, cancellationToken);
    }
}
