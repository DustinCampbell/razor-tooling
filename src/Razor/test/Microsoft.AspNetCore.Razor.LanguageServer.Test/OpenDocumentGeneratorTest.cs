// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common.LanguageServer;
using Microsoft.AspNetCore.Razor.Test.Common.Workspaces;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer;

public class OpenDocumentGeneratorTest(ITestOutputHelper testOutput) : LanguageServerTestBase(testOutput)
{
    private readonly HostDocument[] _documents =
    [
        new HostDocument(
            filePath: "c:/Test1/Index.cshtml",
            targetPath: "Index.cshtml"),
        new HostDocument(
            filePath: "c:/Test1/Components/Counter.cshtml",
            targetPath: "Components/Counter.cshtml"),
    ];

    private static readonly HostProject s_hostProject1 = new(
        filePath: "c:/Test1/Test1.csproj",
        intermediateOutputPath: "c:/Test1/obj",
        RazorConfiguration.Default,
        "TestRootNamespace");

    private static readonly HostProject s_hostProject2 = new(
        filePath: "c:/Test2/Test2.csproj",
        intermediateOutputPath: "c:/Test2/obj",
        RazorConfiguration.Default,
        "TestRootNamespace");

    [Fact]
    public async Task DocumentAdded_ProcessesOpenDocument()
    {
        // Arrange
        var projectManager = CreateProjectSnapshotManager(new LspProjectEngineFactoryProvider(TestRazorLSPOptionsMonitor.Create()));
        var listener = new TestDocumentProcessedListener();
        using var generator = CreateOpenDocumentGenerator(projectManager, listener);

        await projectManager.UpdateAsync(updater =>
        {
            updater.ProjectAdded(s_hostProject1);
            updater.ProjectAdded(s_hostProject2);
            updater.DocumentAdded(s_hostProject1.Key, _documents[0], null!);
            updater.DocumentOpened(s_hostProject1.Key, _documents[0].FilePath, SourceText.From(string.Empty));
        });

        await listener.GetProcessedDocumentAsync(cancelAfter: TimeSpan.FromSeconds(10));
        listener.Reset();

        await projectManager.UpdateAsync(updater =>
        {
            updater.DocumentRemoved(s_hostProject1.Key, _documents[0]);
            updater.DocumentAdded(s_hostProject2.Key, _documents[0], null!);
        });

        // Assert
        var document = await listener.GetProcessedDocumentAsync(cancelAfter: TimeSpan.FromSeconds(10));
        Assert.Equal(s_hostProject2.Key, document.Project.Key);
        Assert.Equal(_documents[0].FilePath, document.FilePath);
    }

    [Fact]
    public async Task DocumentAdded_IgnoresClosedDocument()
    {
        // Arrange
        var projectManager = CreateProjectSnapshotManager();
        var listener = new TestDocumentProcessedListener();
        using var generator = CreateOpenDocumentGenerator(projectManager, listener);

        await projectManager.UpdateAsync(updater =>
        {
            updater.ProjectAdded(s_hostProject1);
            updater.ProjectAdded(s_hostProject2);

            // Act
            updater.DocumentAdded(s_hostProject1.Key, _documents[0], null!);
        });

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => listener.GetProcessedDocumentAsync(cancelAfter: TimeSpan.FromMilliseconds(50)));
    }

    [Fact]
    public async Task DocumentChanged_IgnoresClosedDocument()
    {
        // Arrange
        var projectManager = CreateProjectSnapshotManager();
        var listener = new TestDocumentProcessedListener();
        using var generator = CreateOpenDocumentGenerator(projectManager, listener);

        await projectManager.UpdateAsync(updater =>
        {
            updater.ProjectAdded(s_hostProject1);
            updater.ProjectAdded(s_hostProject2);
            updater.DocumentAdded(s_hostProject1.Key, _documents[0], null!);

            // Act
            updater.DocumentChanged(s_hostProject1.Key, _documents[0].FilePath, SourceText.From("new"));
        });

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => listener.GetProcessedDocumentAsync(cancelAfter: TimeSpan.FromMilliseconds(50)));
    }

    [Fact]
    public async Task DocumentChanged_ProcessesOpenDocument()
    {
        // Arrange
        var projectManager = CreateProjectSnapshotManager();
        var listener = new TestDocumentProcessedListener();
        using var generator = CreateOpenDocumentGenerator(projectManager, listener);

        await projectManager.UpdateAsync(updater =>
        {
            updater.ProjectAdded(s_hostProject1);
            updater.ProjectAdded(s_hostProject2);
            updater.DocumentAdded(s_hostProject1.Key, _documents[0], null!);
            updater.DocumentOpened(s_hostProject1.Key, _documents[0].FilePath, SourceText.From(string.Empty));

            // Act
            updater.DocumentChanged(s_hostProject1.Key, _documents[0].FilePath, SourceText.From("new"));
        });

        // Assert

        var document = await listener.GetProcessedDocumentAsync(cancelAfter: TimeSpan.FromSeconds(10));
        Assert.Equal(document.FilePath, _documents[0].FilePath);
    }

    [Fact]
    public async Task ProjectChanged_IgnoresClosedDocument()
    {
        // Arrange
        var projectManager = CreateProjectSnapshotManager();
        var listener = new TestDocumentProcessedListener();
        using var generator = CreateOpenDocumentGenerator(projectManager, listener);

        await projectManager.UpdateAsync(updater =>
        {
            updater.ProjectAdded(s_hostProject1);
            updater.ProjectAdded(s_hostProject2);
            updater.DocumentAdded(s_hostProject1.Key, _documents[0], null!);

            // Act
            updater.ProjectWorkspaceStateChanged(s_hostProject1.Key,
                ProjectWorkspaceState.Create(LanguageVersion.CSharp8));
        });

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => listener.GetProcessedDocumentAsync(cancelAfter: TimeSpan.FromMilliseconds(50)));
    }

    [Fact]
    public async Task ProjectChanged_ProcessesOpenDocument()
    {
        // Arrange
        var projectManager = CreateProjectSnapshotManager();
        var listener = new TestDocumentProcessedListener();
        using var generator = CreateOpenDocumentGenerator(projectManager, listener);

        await projectManager.UpdateAsync(updater =>
        {
            updater.ProjectAdded(s_hostProject1);
            updater.ProjectAdded(s_hostProject2);
            updater.DocumentAdded(s_hostProject1.Key, _documents[0], null!);
            updater.DocumentOpened(s_hostProject1.Key, _documents[0].FilePath, SourceText.From(string.Empty));

            // Act
            updater.ProjectWorkspaceStateChanged(s_hostProject1.Key,
                ProjectWorkspaceState.Create(LanguageVersion.CSharp8));
        });

        // Assert
        var document = await listener.GetProcessedDocumentAsync(cancelAfter: TimeSpan.FromSeconds(10));
        Assert.Equal(document.FilePath, _documents[0].FilePath);
    }

    private OpenDocumentGenerator CreateOpenDocumentGenerator(
        IProjectSnapshotManager projectManager,
        params IDocumentProcessedListener[] listeners)
    {
        return new OpenDocumentGenerator(listeners, projectManager, TestLanguageServerFeatureOptions.Instance, LoggerFactory);
    }

    private class TestDocumentProcessedListener : IDocumentProcessedListener
    {
        private TaskCompletionSource<IDocumentSnapshot> _tcs;

        public TestDocumentProcessedListener()
        {
            _tcs = new TaskCompletionSource<IDocumentSnapshot>();
        }

        public Task<IDocumentSnapshot> GetProcessedDocumentAsync(TimeSpan cancelAfter)
        {
            var cts = new CancellationTokenSource(cancelAfter);
            var registration = cts.Token.Register(() => _tcs.SetCanceled());
            _ = _tcs.Task.ContinueWith(
                (t) =>
                {
                    registration.Dispose();
                    cts.Dispose();
                },
                TaskScheduler.Current);

            return _tcs.Task;
        }

        public void DocumentProcessed(RazorCodeDocument codeDocument, IDocumentSnapshot document)
        {
            _tcs.SetResult(document);
        }

        internal void Reset()
        {
            _tcs = new TaskCompletionSource<IDocumentSnapshot>();
        }
    }
}
