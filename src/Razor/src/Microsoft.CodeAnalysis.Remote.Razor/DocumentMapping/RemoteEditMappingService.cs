// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;

[Export(typeof(IEditMappingService)), Shared]
[method: ImportingConstructor]
internal sealed class RemoteEditMappingService(
    IDocumentMappingService documentMappingService,
    IFilePathService filePathService) : AbstractEditMappingService(documentMappingService, filePathService)
{
    protected override bool TryGetDocumentContext(IRazorDocument contextDocument, Uri razorDocumentUri, VSProjectContext? projectContext, [NotNullWhen(true)] out DocumentContext? documentContext)
    {
        var solution = contextDocument.ToRemoteRazorDocument().Project.Solution;
        if (!solution.TryGetDocument(razorDocumentUri, out var document))
        {
            documentContext = null;
            return false;
        }

        documentContext = new RemoteDocumentContext(razorDocumentUri, document);
        return true;
    }
}
