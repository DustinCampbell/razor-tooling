// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.AspNetCore.Razor.LanguageServer.Hosting;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Telemetry;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.DependencyInjection;
using Nerdbank.Streams;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks.LanguageServer;

public class RazorLanguageServerBenchmarkBase : ProjectSnapshotManagerBenchmarkBase
{
    public RazorLanguageServerBenchmarkBase()
    {
        var (_, serverStream) = FullDuplexStream.CreatePair();
        var razorLoggerFactory = EmptyLoggerFactory.Instance;
        Logger = razorLoggerFactory.GetOrCreateLogger(GetType());
        RazorLanguageServerHost = RazorLanguageServerHost.Create(
            serverStream,
            serverStream,
            razorLoggerFactory,
            NoOpTelemetryReporter.Instance,
            configureServices: (collection) =>
            {
                collection.AddSingleton<IOnInitialized, NoopClientNotifierService>();
                collection.AddSingleton<IClientConnection, NoopClientNotifierService>();
                collection.AddSingleton<IRazorProjectInfoDriver, NoopRazorProjectInfoDriver>();
                Builder(collection);
            },
            featureOptions: BuildFeatureOptions());
    }

    protected internal virtual void Builder(IServiceCollection collection)
    {
    }

    private protected virtual LanguageServerFeatureOptions? BuildFeatureOptions()
    {
        return null;
    }

    private protected RazorLanguageServerHost RazorLanguageServerHost { get; }

    private protected ILogger Logger { get; }

    internal static async Task<IDocumentSnapshot> GetDocumentSnapshotAsync(string projectFilePath, string filePath, string targetPath, string? rootNamespace = null)
    {
        var intermediateOutputPath = Path.Combine(Path.GetDirectoryName(projectFilePath).AssumeNotNull(), "obj");
        var hostProject = new HostProject(
            projectFilePath,
            intermediateOutputPath,
            RazorConfiguration.Default,
            rootNamespace);

        using var fileStream = new FileStream(filePath, FileMode.Open);
        var text = SourceText.From(fileStream);
        var textLoader = TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create()));
        var hostDocument = new HostDocument(filePath, targetPath, FileKinds.Component);

        var projectEngineFactoryProvider = rootNamespace is not null
            ? new ProjectEngineFactoryProvider(rootNamespace)
            : null;

        using var projectManager = CreateProjectSnapshotManager(projectEngineFactoryProvider);

        await projectManager.UpdateAsync(
            updater =>
            {
                updater.ProjectAdded(hostProject);
                var tagHelpers = CommonResources.LegacyTagHelpers;
                var projectWorkspaceState = ProjectWorkspaceState.Create(tagHelpers, CodeAnalysis.CSharp.LanguageVersion.CSharp11);
                updater.ProjectWorkspaceStateChanged(hostProject.Key, projectWorkspaceState);
                updater.DocumentAdded(hostProject.Key, hostDocument, textLoader);
            },
            CancellationToken.None);

        var projectSnapshot = projectManager.GetLoadedProject(hostProject.Key);

        return projectSnapshot.GetDocument(filePath).AssumeNotNull();
    }

    private sealed class ProjectEngineFactoryProvider(string rootNamespace) : IProjectEngineFactoryProvider
    {
        public IProjectEngineFactory GetFactory(RazorConfiguration configuration)
            => new Factory(rootNamespace);

        private sealed class Factory(string rootNamespace) : IProjectEngineFactory
        {
            public string ConfigurationName => "FactoryWithRootNamespace";

            public RazorProjectEngine Create(RazorConfiguration configuration, RazorProjectFileSystem fileSystem, Action<RazorProjectEngineBuilder>? configure)
                => RazorProjectEngine.Create(builder =>
                {
                    configure?.Invoke(builder);
                    builder.SetRootNamespace(rootNamespace);
                });
        }
    }

    private class NoopClientNotifierService : IClientConnection, IOnInitialized
    {
        public Task OnInitializedAsync(ILspServices services, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SendNotificationAsync<TParams>(string method, TParams @params, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SendNotificationAsync(string method, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<TResponse> SendRequestAsync<TParams, TResponse>(string method, TParams @params, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class NoopRazorProjectInfoDriver : IRazorProjectInfoDriver
    {
        public void AddListener(IRazorProjectInfoListener listener)
        {
        }

        public ImmutableArray<RazorProjectInfo> GetLatestProjectInfo()
            => [];

        public Task WaitForInitializationAsync()
            => Task.CompletedTask;
    }
}
