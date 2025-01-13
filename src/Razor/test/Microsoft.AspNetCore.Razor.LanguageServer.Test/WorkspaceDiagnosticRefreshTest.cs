// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Hosting;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.LanguageServer;
using Microsoft.AspNetCore.Razor.Test.Common.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test;

public class WorkspaceDiagnosticRefreshTest(ITestOutputHelper testOutputHelper) : LanguageServerTestBase(testOutputHelper)
{
    private static readonly TimeSpan s_delay = TimeSpan.FromMilliseconds(10);

    [Fact]
    public async Task WorkspaceRefreshSent()
    {
        var solutionManager = CreateSolutionManager();
        var clientConnection = new StrictMock<IClientConnection>();
        clientConnection
            .Setup(c => c.SendNotificationAsync(Methods.WorkspaceDiagnosticRefreshName, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        using var publisher = new WorkspaceDiagnosticsRefresher(
            solutionManager,
            new TestClientCapabilitiesService(new()
            {
                Workspace = new()
                {
                    Diagnostics = new()
                    {
                        RefreshSupport = true
                    }
                }
            }),
            clientConnection.Object,
            s_delay);

        var testAccessor = publisher.GetTestAccessor();

        await solutionManager.UpdateAsync(
            static updater =>
            {
                var hostProject = TestHostProject.Create("C:/path/to/project.csproj");
                updater.AddProject(hostProject);
            });

        await testAccessor.WaitForRefreshAsync();

        clientConnection.Verify();
    }

    [Fact]
    public async Task WorkspaceRefreshSent_MultipleTimes()
    {
        var solutionManager = CreateSolutionManager();
        var clientConnection = new StrictMock<IClientConnection>();
        clientConnection
            .Setup(c => c.SendNotificationAsync(Methods.WorkspaceDiagnosticRefreshName, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var publisher = new WorkspaceDiagnosticsRefresher(
            solutionManager,
            new TestClientCapabilitiesService(new()
            {
                Workspace = new()
                {
                    Diagnostics = new()
                    {
                        RefreshSupport = true
                    }
                }
            }),
            clientConnection.Object,
            s_delay);

        var testAccessor = publisher.GetTestAccessor();

        var hostProject = TestHostProject.Create("C:/path/to/project.csproj");

        var directory = Path.GetDirectoryName(hostProject.FilePath);
        Assert.NotNull(directory);

        var hostDocument = TestHostDocument.Create(hostProject, Path.Combine(directory, "directory.razor"));

        await solutionManager.UpdateAsync(
            updater =>
            {
                updater.AddProject(hostProject);
            });

        await testAccessor.WaitForRefreshAsync();

        await solutionManager.UpdateAsync(
            updater =>
            {
                updater.AddDocument(hostProject.Key, hostDocument, EmptyTextLoader.Instance);
            });

        await testAccessor.WaitForRefreshAsync();

        clientConnection.Verify(
            c => c.SendNotificationAsync(Methods.WorkspaceDiagnosticRefreshName, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task WorkspaceRefreshNotSent_ClientDoesNotSupport()
    {
        var solutionManager = CreateSolutionManager();
        var clientConnection = new StrictMock<IClientConnection>();

        using var publisher = new WorkspaceDiagnosticsRefresher(
            solutionManager,
            new TestClientCapabilitiesService(new()
            {
                Workspace = new()
                {
                    Diagnostics = new()
                    {
                        RefreshSupport = false
                    }
                }
            }),
            clientConnection.Object,
            s_delay);

        var testAccessor = publisher.GetTestAccessor();

        await solutionManager.UpdateAsync(
            updater =>
            {
                var hostProject = TestHostProject.Create("C:/path/to/project.csproj");
                updater.AddProject(hostProject);
            });

        await testAccessor.WaitForRefreshAsync();

        clientConnection
            .Verify(c => c.SendNotificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Never);
    }

    [Fact]
    public async Task WorkspaceRefreshNotSent_RefresherDisposed()
    {
        var solutionManager = CreateSolutionManager();
        var clientConnection = new StrictMock<IClientConnection>();

        var publisher = new WorkspaceDiagnosticsRefresher(
            solutionManager,
            new TestClientCapabilitiesService(new()
            {
                Workspace = new()
                {
                    Diagnostics = new()
                    {
                        RefreshSupport = false
                    }
                }
            }),
            clientConnection.Object,
            s_delay);

        var testAccessor = publisher.GetTestAccessor();

        publisher.Dispose();

        await solutionManager.UpdateAsync(
            static updater =>
            {
                var hostProject = TestHostProject.Create("C:/path/to/project.csproj");
                updater.AddProject(hostProject);
            });

        await testAccessor.WaitForRefreshAsync();

        clientConnection
            .Verify(c => c.SendNotificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Never);
    }
}
