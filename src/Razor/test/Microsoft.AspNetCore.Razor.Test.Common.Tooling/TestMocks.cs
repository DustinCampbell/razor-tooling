// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Moq;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal static class TestMocks
{
    public interface IMockBuilder<T>
        where T : class
    {
        StrictMock<T> Mock { get; }
    }

    private abstract class MockBuilder<T>
        where T : class
    {
        public StrictMock<T> Mock { get; }

        protected MockBuilder()
        {
            Mock = new();
        }

        public virtual void CompleteMock()
        {
        }
    }

    public static TextLoader CreateTextLoader(string filePath, string text)
    {
        return CreateTextLoader(filePath, SourceText.From(text));
    }

    public static TextLoader CreateTextLoader(string filePath, SourceText text)
    {
        var mock = new StrictMock<TextLoader>();

        var textAndVersion = TextAndVersion.Create(text, VersionStamp.Create(), filePath);

        mock.Setup(x => x.LoadTextAndVersionAsync(It.IsAny<LoadTextOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(textAndVersion);

        return mock.Object;
    }

    public static IProjectSnapshot CreateProjectSnapshot(Action<IProjectSnapshotBuilder> configure)
    {
        var builder = new ProjectSnapshotBuilder();
        configure.Invoke(builder);
        builder.CompleteMock();

        return builder.Mock.Object;
    }

    public interface IProjectSnapshotBuilder : IMockBuilder<IProjectSnapshot>
    {
        void AddDocument(IDocumentSnapshot document);
        void SetEmptyDocuments();

        void SetConfiguration(RazorConfiguration configuration);
        void SetCSharpLanguageVersion(LanguageVersion csharpLanguageVersion);
        void SetDisplayName(string displayName);
        void SetFilePath(string filePath);
        void SetIntermediateOutputPath(string intermediateOutputPath);
        void SetKey(ProjectKey key);
        void SetProjectEngine(RazorProjectEngine projectEngine);
        void SetProjectWorkspaceState(ProjectWorkspaceState projectWorkspaceState);
        void SetRootNamespace(string? rootNamespace);
        void SetTagHelpers(params ImmutableArray<TagHelperDescriptor> tagHelpers);
        void SetVersion(VersionStamp version);
    }

    private sealed class ProjectSnapshotBuilder : MockBuilder<IProjectSnapshot>, IProjectSnapshotBuilder
    {
        private List<IDocumentSnapshot>? _documents;

        public void AddDocument(IDocumentSnapshot document)
        {
            _documents ??= [];
            _documents.Add(document);
        }

        public void SetEmptyDocuments()
        {
            if (_documents is null)
            {
                _documents = [];
            }
            else
            {
                _documents.Clear();
            }
        }

        public void SetConfiguration(RazorConfiguration configuration)
        {
            Mock.SetupGet(x => x.Configuration)
                .Returns(configuration);
        }

        public void SetCSharpLanguageVersion(LanguageVersion csharpLanguageVersion)
        {
            Mock.SetupGet(x => x.CSharpLanguageVersion)
                .Returns(csharpLanguageVersion);
        }

        public void SetDisplayName(string displayName)
        {
            Mock.SetupGet(x => x.DisplayName)
                .Returns(displayName);
        }

        public void SetFilePath(string filePath)
        {
            Mock.SetupGet(x => x.FilePath)
                .Returns(filePath);
        }

        public void SetIntermediateOutputPath(string intermediateOutputPath)
        {
            Mock.SetupGet(x => x.IntermediateOutputPath)
                .Returns(intermediateOutputPath);
        }

        public void SetKey(ProjectKey key)
        {
            Mock.SetupGet(x => x.Key)
                .Returns(key);
        }

        public void SetProjectEngine(RazorProjectEngine projectEngine)
        {
            Mock.Setup(x => x.GetProjectEngine())
                .Returns(projectEngine);
        }

        public void SetProjectWorkspaceState(ProjectWorkspaceState projectWorkspaceState)
        {
            Mock.SetupGet(x => x.ProjectWorkspaceState)
                .Returns(projectWorkspaceState);
        }

        public void SetRootNamespace(string? rootNamespace)
        {
            Mock.SetupGet(x => x.RootNamespace)
                .Returns(rootNamespace);
        }

        public void SetTagHelpers(params ImmutableArray<TagHelperDescriptor> tagHelpers)
        {
            Mock.Setup(x => x.GetTagHelpersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(tagHelpers);
        }

        public void SetVersion(VersionStamp version)
        {
            Mock.SetupGet(x => x.Version)
                .Returns(version);
        }

        public override void CompleteMock()
        {
            if (_documents is null)
            {
                return;
            }

            if (_documents.Count == 0)
            {
                Mock.SetupGet(x => x.DocumentFilePaths)
                    .Returns([]);

                Mock.Setup(x => x.GetDocument(It.IsAny<string>()))
                    .Returns<IDocumentSnapshot?>(null);

                IDocumentSnapshot? outDocument = null;
                Mock.Setup(x => x.TryGetDocument(It.IsAny<string>(), out outDocument))
                    .Returns(false);
            }
            else
            {
                Mock.SetupGet(x => x.DocumentFilePaths)
                    .Returns([.. _documents.Select(d => d.FilePath.AssumeNotNull())]);

                foreach (var document in _documents)
                {
                    var filePath = document.FilePath.AssumeNotNull();

                    Mock.Setup(x => x.GetDocument(filePath))
                        .Returns(document);

                    var outDocument = document;
                    Mock.Setup(x => x.TryGetDocument(filePath, out outDocument))
                        .Returns(true);
                }
            }
        }
    }
}
