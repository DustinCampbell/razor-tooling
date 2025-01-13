// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer;

internal sealed class DocumentContextFactory(
    RazorSolutionManager solutionManager,
    ILoggerFactory loggerFactory)
    : IDocumentContextFactory
{
    private readonly RazorSolutionManager _solutionManager = solutionManager;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<DocumentContextFactory>();

    public bool TryCreate(
        Uri documentUri,
        VSProjectContext? projectContext,
        [NotNullWhen(true)] out DocumentContext? context)
    {
        var filePath = documentUri.GetAbsoluteOrUNCPath();

        if (!TryResolveDocument(filePath, projectContext, out var document))
        {
            // Stale request or misbehaving client, see above comment.
            context = null;
            return false;
        }

        context = new DocumentContext(documentUri, document, projectContext);
        return true;
    }

    private bool TryResolveDocument(
        string filePath,
        VSProjectContext? projectContext,
        [NotNullWhen(true)] out RazorDocument? document)
    {
        if (projectContext is null)
        {
            return _solutionManager.TryResolveDocumentInAnyProject(filePath, _logger, out document);
        }

        if (_solutionManager.TryGetProject(projectContext.ToProjectKey(), out var project) &&
            project.TryGetDocument(filePath, out document))
        {
            return true;
        }

        // Couldn't find the document in a real project. Maybe the language server doesn't yet know about the project
        // that the IDE is asking us about. In that case, we might have the document in our misc files project, and we'll
        // move it to the real project when/if we find out about it.
        var miscellaneousProject = _solutionManager.GetMiscellaneousProject();
        var normalizedDocumentPath = FilePathNormalizer.Normalize(filePath);
        if (miscellaneousProject.TryGetDocument(normalizedDocumentPath, out document))
        {
            _logger.LogDebug($"Found document {filePath} in the misc files project, but was asked for project context {projectContext.Id}");
            return true;
        }

        document = null;
        return false;
    }
}
