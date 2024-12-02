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
using Microsoft.AspNetCore.Razor.Test.Common.Workspaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.NET.Sdk.Razor.SourceGenerators;

namespace Microsoft.AspNetCore.Razor.Test.Common.ProjectSystem;

internal sealed class TestProjectSnapshot : IProjectSnapshot
{
    public ProjectSnapshot RealSnapshot { get; }

    private TestProjectSnapshot(ProjectSnapshot realSnapshot)
    {
        RealSnapshot = realSnapshot;
    }

    public static TestProjectSnapshot Create(string filePath, ProjectWorkspaceState? projectWorkspaceState = null)
    {
        var hostProject = TestHostProject.Create(filePath);

        var solutionState = SolutionState
            .Create(ProjectEngineFactories.DefaultProvider, TestLanguageServerFeatureOptions.Instance)
            .AddProject(hostProject);

        if (projectWorkspaceState is not null)
        {
            solutionState = solutionState.UpdateProjectWorkspaceState(hostProject.Key, projectWorkspaceState);
        }

        var solution = new SolutionSnapshot(solutionState);
        var project = solution.GetLoadedProject(hostProject.Key);

        return new TestProjectSnapshot(project);
    }

    public HostProject HostProject => RealSnapshot.HostProject;

    public ISolutionSnapshot Solution => RealSnapshot.Solution;
    public ProjectKey Key => RealSnapshot.Key;
    public RazorConfiguration Configuration => RealSnapshot.Configuration;
    public IEnumerable<string> DocumentFilePaths => RealSnapshot.DocumentFilePaths;
    public string FilePath => RealSnapshot.FilePath;
    public string IntermediateOutputPath => RealSnapshot.IntermediateOutputPath;
    public string? RootNamespace => RealSnapshot.RootNamespace;
    public string DisplayName => RealSnapshot.DisplayName;
    public LanguageVersion CSharpLanguageVersion => RealSnapshot.CSharpLanguageVersion;
    public ProjectWorkspaceState ProjectWorkspaceState => RealSnapshot.ProjectWorkspaceState;
    public VersionStamp Version => RealSnapshot.Version;

    public RazorProjectEngine GetProjectEngine()
        => RazorProjectEngine.Create(
            Configuration,
            RazorProjectFileSystem.Create("C:/"),
            b => b.Features.Add(new ConfigureRazorParserOptions(useRoslynTokenizer: true, CSharpParseOptions.Default)));

    public ValueTask<ImmutableArray<TagHelperDescriptor>> GetTagHelpersAsync(CancellationToken cancellationToken)
        => RealSnapshot.GetTagHelpersAsync(cancellationToken);

    public bool ContainsDocument(string filePath)
        => RealSnapshot.ContainsDocument(filePath);

    public IDocumentSnapshot? GetDocument(string filePath)
        => RealSnapshot.GetDocument(filePath);

    public bool TryGetDocument(string filePath, [NotNullWhen(true)] out IDocumentSnapshot? document)
    {
        if (RealSnapshot.TryGetDocument(filePath, out var snapshot))
        {
            document = snapshot;
            return true;
        }

        document = null;
        return false;
    }
}
