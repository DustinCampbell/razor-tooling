// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

internal sealed class RemoteDocumentContext : DocumentContext
{
    public TextDocument TextDocument => Document.TextDocument;

    public new RemoteRazorDocument Document => (RemoteRazorDocument)base.Document;

    public ISolutionQueryOperations GetSolutionQueryOperations()
        => Document.Project.Solution;

    public RemoteDocumentContext(Uri uri, RemoteRazorDocument document)
        // HACK: Need to revisit projectContext here I guess
        : base(uri, document, projectContext: null)
    {
    }
}
