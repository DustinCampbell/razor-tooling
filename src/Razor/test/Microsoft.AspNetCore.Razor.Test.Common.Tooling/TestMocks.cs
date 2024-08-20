// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Moq;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal static class TestMocks
{
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

    public static IProjectSnapshot CreateProjectSnapshot(Action<IProjectSnapshotBuilder>? configure = null)
    {
        var builder = new ProjectSnapshotBuilder();
        configure?.Invoke(builder);

        return builder.Mock.Object;
    }

    public interface IProjectSnapshotBuilder : IMockBuilder<IProjectSnapshot>
    {
        void SetupKey(ProjectKey value);
        void SetupConfiguration(RazorConfiguration value);
        void SetupFilePath(string value);
        void SetupVersion(VersionStamp value = default);

        void SetupProjectEngine(RazorProjectEngine value);
        void SetupTagHelpers(params ImmutableArray<TagHelperDescriptor> value);

        void AddDocument(string filePath, Action<IDocumentSnapshotBuilder>? configure = null);
    }

    private sealed class ProjectSnapshotBuilder : MockBuilder<IProjectSnapshot>, IProjectSnapshotBuilder
    {
        private List<string>? _documentFilePaths;

        public void SetupKey(ProjectKey value)
        {
            Mock.SetupGet(x => x.Key)
                .Returns(value);
        }

        public void SetupConfiguration(RazorConfiguration value)
        {
            Mock.SetupGet(x => x.Configuration)
                .Returns(value);
        }

        public void SetupFilePath(string value)
        {
            Mock.SetupGet(x => x.FilePath)
                .Returns(value);
        }

        public void SetupVersion(VersionStamp value = default)
        {
            Mock.SetupGet(x => x.Version)
                .Returns(value);
        }

        public void SetupProjectEngine(RazorProjectEngine value)
        {
            Mock.Setup(x => x.GetProjectEngine())
                .Returns(value);
        }

        public void SetupTagHelpers(params ImmutableArray<TagHelperDescriptor> value)
        {
            Mock.Setup(x => x.GetTagHelpersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(value);
        }

        public void AddDocument(string filePath, Action<IDocumentSnapshotBuilder>? configure = null)
        {
            var builder = new DocumentSnapshotBuilder();
            builder.SetupFilePath(filePath);

            configure?.Invoke(builder);

            if (_documentFilePaths is null)
            {
                _documentFilePaths = [];

                Mock.SetupGet(x => x.DocumentFilePaths)
                    .Returns(() => _documentFilePaths);
            }

            Mock.Setup(x => x.GetDocument(filePath))
                .Returns(() => builder.Mock.Object);

            var documentOut = builder.Mock.Object;
            Mock.Setup(x => x.TryGetDocument(filePath, out documentOut))
                .Returns(true);
        }
    }

    public static IDocumentSnapshot CreateDocumentSnapshot(Action<IDocumentSnapshotBuilder>? configure = null)
    {
        var builder = new DocumentSnapshotBuilder();
        configure?.Invoke(builder);

        return builder.Mock.Object;
    }

    public interface IDocumentSnapshotBuilder : IMockBuilder<IDocumentSnapshot>
    {
        void SetupFileKind(string value);
        void SetupFilePath(string value);
        void SetupTargetPath(string value);
        void SetupProject(IProjectSnapshot value);
        void SetupProject(Action<IProjectSnapshotBuilder> configure);

        void SetupGeneratedOutput(RazorCodeDocument value, bool setupText = true);
        void SetupText(SourceText value);

        void SetupWithText(Func<SourceText, IDocumentSnapshot> function);
    }

    private sealed class DocumentSnapshotBuilder : MockBuilder<IDocumentSnapshot>, IDocumentSnapshotBuilder
    {
        public void SetupFileKind(string value)
        {
            Mock.SetupGet(x => x.FileKind)
                .Returns(value);
        }

        public void SetupFilePath(string value)
        {
            Mock.SetupGet(x => x.FilePath)
                .Returns(value);
        }

        public void SetupTargetPath(string value)
        {
            Mock.SetupGet(x => x.TargetPath)
                .Returns(value);
        }

        public void SetupProject(IProjectSnapshot value)
        {
            Mock.SetupGet(x => x.Project)
                .Returns(value);
        }

        public void SetupProject(Action<IProjectSnapshotBuilder> configure)
        {
            var projectSnapshot = CreateProjectSnapshot(configure);

            Mock.SetupGet(x => x.Project)
                .Returns(projectSnapshot);
        }

        public void SetupGeneratedOutput(RazorCodeDocument value, bool setupText = true)
        {
            Mock.Setup(x => x.GetGeneratedOutputAsync())
                .ReturnsAsync(value);

            if (setupText)
            {
                SetupText(value.Source.Text);
            }
        }

        public void SetupText(SourceText value)
        {
            Mock.Setup(x => x.GetTextAsync())
                .ReturnsAsync(value);
        }

        public void SetupWithText(Func<SourceText, IDocumentSnapshot> function)
        {
            Mock.Setup(x => x.WithText(It.IsAny<SourceText>()))
                .Returns(function);
        }
    }
}
