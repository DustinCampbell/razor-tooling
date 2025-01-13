// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Composition;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Razor.Telemetry;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

[Shared]
[Export(typeof(RemoteSnapshotManager))]
[method: ImportingConstructor]
internal sealed class RemoteSnapshotManager(LanguageServerFeatureOptions languageServerFeatureOptions, IFilePathService filePathService, ITelemetryReporter telemetryReporter)
{
    private static readonly ConditionalWeakTable<Solution, RemoteRazorSolution> s_solutionMap = new();

    public RazorCompilerOptions CompilerOptions { get; } = languageServerFeatureOptions.ToCompilerOptions();
    public IFilePathService FilePathService { get; } = filePathService;
    public ITelemetryReporter TelemetryReporter { get; } = telemetryReporter;

    public RemoteRazorSolution GetSolution(Solution solution)
    {
        return s_solutionMap.GetValue(solution, s => new RemoteRazorSolution(s, this));
    }

    public RemoteRazorProject GetProject(Project project)
    {
        return GetSolution(project.Solution).GetProject(project);
    }

    public RemoteRazorDocument GetDocument(TextDocument textDocument)
    {
        return GetProject(textDocument.Project).GetDocument(textDocument);
    }
}
