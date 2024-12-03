// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal interface ISolutionSnapshot
{
    IEnumerable<IProjectSnapshot> Projects { get; }

    ImmutableArray<ProjectKey> GetProjectKeysWithFilePath(string filePath);

    bool TryGetProject(ProjectKey projectKey, [NotNullWhen(true)] out IProjectSnapshot? project);
    bool TryGetDocument(ProjectKey projectKey, string documentFilePath, [NotNullWhen(true)] out IDocumentSnapshot? document);
}
