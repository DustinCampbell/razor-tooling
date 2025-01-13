// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

internal sealed class RemoteDocumentContext(Uri uri, RemoteRazorDocument document)
    // HACK: Need to revisit projectContext here I guess
    : DocumentContext(uri, document, projectContext: null)
{
    public new RemoteRazorDocument Document => base.Document.ToRemoteRazorDocument();

    public ISolutionQueryOperations GetSolutionQueryOperations()
        => Document.Project.Solution;
}
