// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

internal static class Extensions
{
    private const string RazorExtension = ".razor";
    private const string CSHtmlExtension = ".cshtml";

    public static bool IsRazorFilePath(this string filePath)
    {
        var comparison = FilePathComparison.Instance;

        return filePath.EndsWith(RazorExtension, comparison) ||
               filePath.EndsWith(CSHtmlExtension, comparison);
    }

    public static bool IsRazorDocument(this TextDocument document)
        => document is AdditionalDocument &&
           document.FilePath is string filePath &&
           filePath.IsRazorFilePath();

    public static bool ContainsRazorDocuments(this Project project)
        => project.AdditionalDocuments.Any(static d => d.IsRazorDocument());

    public static RemoteRazorProject ToRemoteRazorProject(this IRazorProject project)
    {
        if (project is RemoteRazorProject remoteProject)
        {
            return remoteProject;
        }

        return ThrowHelper.ThrowArgumentException<RemoteRazorProject>(nameof(project), $"Project must be an instance of {nameof(RemoteRazorProject)}.");
    }

    public static RemoteRazorDocument ToRemoteRazorDocument(this IRazorDocument document)
    {
        if (document is RemoteRazorDocument remoteDocument)
        {
            return remoteDocument;
        }

        return ThrowHelper.ThrowArgumentException<RemoteRazorDocument>(nameof(document), $"Document must be an instance of {nameof(RemoteRazorDocument)}.");
    }

    public static RemoteDocumentContext ToRemoteDocumentContext(this DocumentContext context)
    {
        if (context is RemoteDocumentContext remoteContext)
        {
            return remoteContext;
        }

        return ThrowHelper.ThrowArgumentException<RemoteDocumentContext>(nameof(context), $"DocumentContext must be an instance of {nameof(RemoteDocumentContext)}.");
    }
}
