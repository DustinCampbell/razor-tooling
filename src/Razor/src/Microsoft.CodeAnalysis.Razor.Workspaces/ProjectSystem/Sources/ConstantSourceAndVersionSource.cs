// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem.Sources;

internal sealed class ConstantSourceAndVersionSource(RazorSourceDocument source, VersionStamp version) : ISourceAndVersionSource
{
    private readonly SourceAndVersion _sourceAndVersion = SourceAndVersion.Create(source, version);

    public TextLoader? TextLoader => null;

    public ValueTask<SourceAndVersion> GetValueAsync(CancellationToken cancellationToken)
        => new(_sourceAndVersion);

    public bool TryGetValue([NotNullWhen(true)] out SourceAndVersion? result)
    {
        result = _sourceAndVersion;
        return true;
    }
}
