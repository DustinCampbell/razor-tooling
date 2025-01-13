// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal sealed class ProjectChangeEventArgs : EventArgs
{
    public ProjectChangeKind Kind { get; }
    public RazorProject? Older { get; }
    public RazorProject? Newer { get; }
    public ProjectKey ProjectKey { get; }
    public string ProjectFilePath { get; }
    public string? DocumentFilePath { get; }
    public bool IsSolutionClosing { get; }

    private ProjectChangeEventArgs(
        ProjectChangeKind kind,
        RazorProject? older,
        RazorProject? newer,
        string? documentFilePath,
        bool isSolutionClosing)
    {
        if (older is null && newer is null)
        {
            ThrowHelper.ThrowInvalidOperationException("Both projects cannot be null.");
        }

        Kind = kind;
        Older = older;
        Newer = newer;
        ProjectKey = (older ?? newer).AssumeNotNull().Key;
        ProjectFilePath = (older ?? newer).AssumeNotNull().FilePath;
        DocumentFilePath = documentFilePath;
        IsSolutionClosing = isSolutionClosing;
    }

    public static ProjectChangeEventArgs ProjectAdded(RazorProject project, bool isSolutionClosing)
        => new(ProjectChangeKind.ProjectAdded, older: null, newer: project, documentFilePath: null, isSolutionClosing);

    public static ProjectChangeEventArgs ProjectRemoved(RazorProject project, bool isSolutionClosing)
        => new(ProjectChangeKind.ProjectRemoved, older: project, newer: null, documentFilePath: null, isSolutionClosing);

    public static ProjectChangeEventArgs ProjectChanged(RazorProject older, RazorProject newer, bool isSolutionClosing)
        => new(ProjectChangeKind.ProjectChanged, older, newer, documentFilePath: null, isSolutionClosing);

    public static ProjectChangeEventArgs DocumentAdded(RazorProject older, RazorProject newer, string documentFilePath, bool isSolutionClosing)
        => new(ProjectChangeKind.DocumentAdded, older, newer, documentFilePath, isSolutionClosing);

    public static ProjectChangeEventArgs DocumentRemoved(RazorProject older, RazorProject newer, string documentFilePath, bool isSolutionClosing)
        => new(ProjectChangeKind.DocumentRemoved, older, newer, documentFilePath, isSolutionClosing);

    public static ProjectChangeEventArgs DocumentChanged(RazorProject older, RazorProject newer, string documentFilePath, bool isSolutionClosing)
        => new(ProjectChangeKind.DocumentChanged, older, newer, documentFilePath, isSolutionClosing);
}
