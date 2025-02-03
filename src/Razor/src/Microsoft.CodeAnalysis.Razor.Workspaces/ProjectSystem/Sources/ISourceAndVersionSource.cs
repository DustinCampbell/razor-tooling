// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem.Sources;

internal interface ISourceAndVersionSource
{
    TextLoader? TextLoader { get; }

    bool TryGetValue([NotNullWhen(true)] out SourceAndVersion? result);
    ValueTask<SourceAndVersion> GetValueAsync(CancellationToken cancellationToken);
}
