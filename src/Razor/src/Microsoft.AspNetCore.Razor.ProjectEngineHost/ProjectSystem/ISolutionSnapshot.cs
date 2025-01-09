// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.ProjectSystem;

internal interface ISolutionSnapshot
{
    IEnumerable<IProjectSnapshot> Projects { get; }

    bool ContainsProject(ProjectKey projectKey);
    bool TryGetProject(ProjectKey projectKey, [NotNullWhen(true)] out IProjectSnapshot? project);

    ImmutableArray<ProjectKey> GetProjectKeysWithFilePath(string filePath);
}
