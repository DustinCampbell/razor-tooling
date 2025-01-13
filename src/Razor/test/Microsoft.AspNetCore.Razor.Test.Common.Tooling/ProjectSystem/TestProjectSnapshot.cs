// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.Test.Common.ProjectSystem;

internal sealed class TestProjectSnapshot : IProjectSnapshot
{
    public RazorProject RealProject { get; }

    private TestProjectSnapshot(ProjectState state)
    {
        RealProject = new RazorProject(state);
    }

    public static TestProjectSnapshot Create(string filePath, ProjectWorkspaceState? projectWorkspaceState = null)
    {
        var hostProject = TestHostProject.Create(filePath);
        var state = ProjectState.Create(hostProject, RazorCompilerOptions.None, ProjectEngineFactories.DefaultProvider);

        if (projectWorkspaceState is not null)
        {
            state = state.WithProjectWorkspaceState(projectWorkspaceState);
        }

        return new TestProjectSnapshot(state);
    }

    public HostProject HostProject => RealProject.HostProject;

    public ProjectKey Key => RealProject.Key;
    public IEnumerable<string> DocumentFilePaths => RealProject.DocumentFilePaths;
    public string FilePath => RealProject.FilePath;
    public string IntermediateOutputPath => RealProject.IntermediateOutputPath;
    public string? RootNamespace => RealProject.RootNamespace;
    public string DisplayName => RealProject.DisplayName;
    public LanguageVersion CSharpLanguageVersion => RealProject.CSharpLanguageVersion;

    public ValueTask<ImmutableArray<TagHelperDescriptor>> GetTagHelpersAsync(CancellationToken cancellationToken)
        => RealProject.GetTagHelpersAsync(cancellationToken);

    public bool ContainsDocument(string filePath)
        => RealProject.ContainsDocument(filePath);

    public bool TryGetDocument(string filePath, [NotNullWhen(true)] out IRazorDocument? document)
    {
        if (RealProject.TryGetDocument(filePath, out var result))
        {
            document = result;
            return true;
        }

        document = null;
        return false;
    }
}
