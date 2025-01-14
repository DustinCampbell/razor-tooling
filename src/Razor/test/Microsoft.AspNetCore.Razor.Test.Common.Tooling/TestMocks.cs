// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Hosting;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common.ProjectSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Moq;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal static class TestMocks
{
    public static TextLoader CreateTextLoader(string text)
        => CreateTextLoader(text, VersionStamp.Create());

    public static TextLoader CreateTextLoader(string text, VersionStamp version)
        => CreateTextLoader(SourceText.From(text), version);

    public static TextLoader CreateTextLoader(SourceText text)
        => CreateTextLoader(text, VersionStamp.Create());

    public static TextLoader CreateTextLoader(SourceText text, VersionStamp version)
    {
        var mock = new StrictMock<TextLoader>();

        var textAndVersion = TextAndVersion.Create(text, version);

        mock.Setup(x => x.LoadTextAndVersionAsync(It.IsAny<LoadTextOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(textAndVersion);

        return mock.Object;
    }

    public interface IClientConnectionBuilder
    {
        void SetupSendRequest<TParams, TResponse>(string method, TResponse response, bool verifiable = false);
        void SetupSendRequest<TParams, TResponse>(string method, TParams @params, TResponse response, bool verifiable = false);
    }

    private sealed class ClientConnectionBuilder : IClientConnectionBuilder
    {
        public StrictMock<IClientConnection> Mock { get; } = new();

        public void SetupSendRequest<TParams, TResponse>(string method, TResponse response, bool verifiable = false)
        {
            var returnsResult = Mock
                .Setup(x => x.SendRequestAsync<TParams, TResponse>(method, It.IsAny<TParams>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            if (verifiable)
            {
                returnsResult.Verifiable();
            }
        }

        public void SetupSendRequest<TParams, TResponse>(string method, TParams @params, TResponse response, bool verifiable = false)
        {
            var returnsResult = Mock
                .Setup(x => x.SendRequestAsync<TParams, TResponse>(method, @params, It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            if (verifiable)
            {
                returnsResult.Verifiable();
            }
        }
    }

    public static IClientConnection CreateClientConnection(Action<IClientConnectionBuilder> configure)
    {
        var builder = new ClientConnectionBuilder();
        configure?.Invoke(builder);
        return builder.Mock.Object;
    }

    public static void VerifySendRequest<TParams, TResponse>(this Mock<IClientConnection> mock, string method, Times times)
        => mock.Verify(x => x.SendRequestAsync<TParams, TResponse>(method, It.IsAny<TParams>(), It.IsAny<CancellationToken>()), times);

    public static void VerifySendRequest<TParams, TResponse>(this Mock<IClientConnection> mock, string method, Func<Times> times)
        => mock.Verify(x => x.SendRequestAsync<TParams, TResponse>(method, It.IsAny<TParams>(), It.IsAny<CancellationToken>()), times);

    public static void VerifySendRequest<TParams, TResponse>(this Mock<IClientConnection> mock, string method, TParams @params, Times times)
        => mock.Verify(x => x.SendRequestAsync<TParams, TResponse>(method, @params, It.IsAny<CancellationToken>()), times);

    public static void VerifySendRequest<TParams, TResponse>(this Mock<IClientConnection> mock, string method, TParams @params, Func<Times> times)
        => mock.Verify(x => x.SendRequestAsync<TParams, TResponse>(method, @params, It.IsAny<CancellationToken>()), times);

    public static IRazorDocument CreateDocument(string filePath, RazorCodeDocument codeDocument)
    {
        var hostProject = TestHostProject.Create(filePath + ".csproj");
        var hostDocument = TestHostDocument.Create(hostProject, filePath);

        var documentMock = new StrictMock<IRazorDocument>();

        documentMock
            .SetupGet(x => x.FilePath)
            .Returns(hostDocument.FilePath);
        documentMock
            .SetupGet(x => x.FileKind)
            .Returns(hostDocument.FileKind);
        documentMock
            .SetupGet(x => x.TargetPath)
            .Returns(hostDocument.TargetPath);
        documentMock
            .SetupGet(x => x.Version)
            .Returns(1);

        var text = codeDocument.Source.Text;
        documentMock
            .Setup(x => x.TryGetText(out text))
            .Returns(true);
        documentMock
            .Setup(x => x.GetTextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(text);

        var generatedOutput = codeDocument;
        documentMock
            .Setup(x => x.TryGetGeneratedOutput(out generatedOutput))
            .Returns(true);
        documentMock
            .Setup(x => x.GetGeneratedOutputAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedOutput);

        documentMock
            .Setup(x => x.GetCSharpSyntaxTreeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((CancellationToken cancellationToken) => codeDocument.GetOrParseCSharpSyntaxTree(cancellationToken));

        var projectWorkspaceState = codeDocument.GetTagHelpers() is { } tagHelpers
            ? ProjectWorkspaceState.Create([.. tagHelpers])
            : ProjectWorkspaceState.Default;

        var project = CreateProject(hostProject, projectWorkspaceState);
        documentMock
            .SetupGet(x => x.Project)
            .Returns(project);

        documentMock
            .Setup(x => x.WithText(It.IsAny<SourceText>()))
            .Returns((SourceText text) =>
            {
                return RazorProject
                    .Create(hostProject, RazorCompilerOptions.None, ProjectEngineFactories.DefaultProvider)
                    .AddDocument(hostDocument, text)
                    .GetRequiredDocument(hostDocument.FilePath);
            });

        return documentMock.Object;
    }

    public static IRazorProject CreateProject(HostProject hostProject, ProjectWorkspaceState? projectWorkspaceState = null)
    {
        var mock = new StrictMock<IRazorProject>();

        mock.SetupGet(x => x.Key)
            .Returns(hostProject.Key);
        mock.SetupGet(x => x.FilePath)
            .Returns(hostProject.FilePath);
        mock.SetupGet(x => x.IntermediateOutputPath)
            .Returns(hostProject.IntermediateOutputPath);
        mock.SetupGet(x => x.RootNamespace)
            .Returns(hostProject.RootNamespace);
        mock.SetupGet(x => x.DisplayName)
            .Returns(hostProject.DisplayName);

        if (projectWorkspaceState is not null)
        {
            mock.SetupGet(x => x.CSharpLanguageVersion)
                .Returns(projectWorkspaceState.CSharpLanguageVersion);
            mock.Setup(x => x.GetTagHelpersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(projectWorkspaceState.TagHelpers);
        }

        return mock.Object;
    }
}
