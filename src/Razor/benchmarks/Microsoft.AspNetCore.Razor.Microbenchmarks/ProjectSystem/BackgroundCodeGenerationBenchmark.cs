// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

public class BackgroundCodeGenerationBenchmark : RazorSolutionManagerBenchmarkBase
{
    [IterationSetup]
    public async Task SetupAsync()
    {
        SolutionManager = CreateSolutionManager();

        await SolutionManager.UpdateAsync(
            updater => updater.AddProject(HostProject),
            CancellationToken.None);

        SolutionManager.Changed += SnapshotManager_Changed;
    }

    [IterationCleanup]
    public void Cleanup()
    {
        SolutionManager.Changed -= SnapshotManager_Changed;

        Tasks.Clear();
    }

    private List<Task> Tasks { get; } = new List<Task>();

    private RazorSolutionManager SolutionManager { get; set; }

    [Benchmark(Description = "Generates the code for 100 files", OperationsPerInvoke = 100)]
    public async Task BackgroundCodeGeneration_Generate100FilesAsync()
    {
        await SolutionManager.UpdateAsync(
            updater =>
            {
                for (var i = 0; i < Documents.Length; i++)
                {
                    updater.AddDocument(HostProject.Key, Documents[i], TextLoaders[i % 4]);
                }
            },
            CancellationToken.None);

        await Task.WhenAll(Tasks);
    }

    private void SnapshotManager_Changed(object sender, ProjectChangeEventArgs e)
    {
        // The real work happens here.
        var document = SolutionManager.GetRequiredDocument(e.ProjectKey, e.DocumentFilePath);

        Tasks.Add(document.GetGeneratedOutputAsync(CancellationToken.None).AsTask());
    }
}
