// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal class ProjectChangeEventArgs : EventArgs
{
    public ProjectChangeKind Kind { get; }
    public IProjectSnapshot? Older { get; }
    public IProjectSnapshot? Newer { get; }
    public ProjectKey ProjectKey { get; }
    public string ProjectFilePath { get; }
    public string? DocumentFilePath { get; }
    public SolutionState SolutionState { get; }

    public ProjectChangeEventArgs(
        ProjectChangeKind kind,
        IProjectSnapshot? older,
        IProjectSnapshot? newer,
        string? documentFilePath,
        SolutionState solutionState)
    {
        if (older is null && newer is null)
        {
            throw new ArgumentException("Both projects cannot be null.");
        }

        Older = older;
        Newer = newer;
        DocumentFilePath = documentFilePath;
        Kind = kind;
        SolutionState = solutionState;
        ProjectFilePath = (older ?? newer)!.FilePath;
        ProjectKey = (older ?? newer)!.Key;
    }

    public override string ToString()
    {
        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        builder.Append('{');
        builder.Append(Kind.ToString());

        if (Kind == ProjectChangeKind.ProjectAdded)
        {
            builder.Append(", ");
            builder.Append(Newer.AssumeNotNull().DisplayName);
        }

        if (Kind == ProjectChangeKind.DocumentAdded)
        {
            builder.Append(", ");
            builder.Append(Path.GetFileName(DocumentFilePath));
            builder.Append(", ");
            builder.Append(Newer.AssumeNotNull().DisplayName);
        }

        builder.Append(", ");
        builder.Append("SolutionState = ");
        builder.Append(SolutionState.ToString());

        builder.Append('}');

        return builder.ToString();
    }
}
