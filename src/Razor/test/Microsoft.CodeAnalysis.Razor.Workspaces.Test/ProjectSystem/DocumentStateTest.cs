// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

public class DocumentStateTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private static readonly HostDocument s_hostDocument = TestProjectData.SomeProjectFile1;
    private static readonly SourceText s_text = SourceText.From("Hello, world!");
    private static readonly TextAndVersion s_textAndVersion = TextAndVersion.Create(s_text, VersionStamp.Create());
    private static readonly TextLoader s_textLoader = TestMocks.CreateTextLoader(s_textAndVersion);

    [Fact]
    public async Task DocumentState_CreatedNew_HasEmptyText()
    {
        // Arrange & Act
        var state = DocumentState.Create(s_hostDocument);

        // Act
        var text = await state.GetTextAsync(DisposalToken);

        // Assert
        Assert.Equal(0, text.Length);
    }

    [Fact]
    public async Task DocumentState_WithText_CreatesNewState()
    {
        // Arrange
        var state = DocumentState.Create(s_hostDocument);

        // Act
        var newState = state.WithText(s_text, VersionStamp.Create());
        var text = await newState.GetTextAsync(DisposalToken);

        // Assert
        Assert.NotSame(state, newState);
        Assert.Same(s_text, text);
    }

    [Fact]
    public async Task DocumentState_WithTextLoader_CreatesNewState()
    {
        // Arrange
        var state = DocumentState.Create(s_hostDocument);

        // Act
        var newState = state.WithTextLoader(s_textLoader);
        var textAndVersion = await newState.GetTextAndVersionAsync(DisposalToken);

        // Assert
        Assert.NotSame(state, newState);
        Assert.Same(s_textAndVersion, textAndVersion);
    }

    [Fact]
    public async Task DocumentState_WithConfigurationChange_CachesSnapshotText()
    {
        // Arrange
        var state = DocumentState.Create(s_hostDocument, s_textAndVersion);
        var textAndVersion = await state.GetTextAndVersionAsync(DisposalToken);

        // Act
        var newState = state.WithConfigurationChange();
        var newTextAndVersion = await newState.GetTextAndVersionAsync(DisposalToken);

        // Assert
        Assert.NotSame(state, newState);
        Assert.Same(textAndVersion, newTextAndVersion);
        Assert.True(newState.TryGetText(out _));
        Assert.True(newState.TryGetTextVersion(out _));
    }

    [Fact]
    public async Task DocumentState_WithConfigurationChange_CachesLoadedText()
    {
        // Arrange
        var state = DocumentState.Create(s_hostDocument, s_textLoader);
        var textAndVersion = await state.GetTextAndVersionAsync(DisposalToken);

        // Act
        var newState = state.WithConfigurationChange();
        var newTextAndVersion = await newState.GetTextAndVersionAsync(DisposalToken);

        // Assert
        Assert.NotSame(state, newState);
        Assert.Same(textAndVersion, newTextAndVersion);
        Assert.True(newState.TryGetText(out _));
        Assert.True(newState.TryGetTextVersion(out _));
    }

    [Fact]
    public async Task DocumentState_WithImportsChange_CachesSnapshotText()
    {
        // Arrange
        var state = DocumentState.Create(s_hostDocument, s_textAndVersion);
        var textAndVersion = await state.GetTextAndVersionAsync(DisposalToken);

        // Act
        var newState = state.WithImportsChange();
        var newTextAndVersion = await newState.GetTextAndVersionAsync(DisposalToken);

        // Assert
        Assert.NotSame(state, newState);
        Assert.Same(textAndVersion, newTextAndVersion);
        Assert.True(newState.TryGetText(out _));
        Assert.True(newState.TryGetTextVersion(out _));
    }

    [Fact]
    public async Task DocumentState_WithImportsChange_CachesLoadedText()
    {
        // Arrange
        var state = DocumentState.Create(s_hostDocument, s_textLoader);
        var textAndVersion = await state.GetTextAndVersionAsync(DisposalToken);

        // Act
        var newState = state.WithImportsChange();
        var newTextAndVersion = await newState.GetTextAndVersionAsync(DisposalToken);

        // Assert
        Assert.NotSame(state, newState);
        Assert.Same(textAndVersion, newTextAndVersion);
        Assert.True(newState.TryGetText(out _));
        Assert.True(newState.TryGetTextVersion(out _));
    }

    [Fact]
    public async Task DocumentState_WithProjectWorkspaceStateChange_CachesSnapshotText()
    {
        // Arrange
        var state = DocumentState.Create(s_hostDocument, s_textAndVersion);
        var textAndVersion = await state.GetTextAndVersionAsync(DisposalToken);

        // Act
        var newState = state.WithProjectWorkspaceStateChange();
        var newTextAndVersion = await newState.GetTextAndVersionAsync(DisposalToken);

        // Assert
        Assert.NotSame(state, newState);
        Assert.Same(textAndVersion, newTextAndVersion);
        Assert.True(newState.TryGetText(out _));
        Assert.True(newState.TryGetTextVersion(out _));
    }

    [Fact]
    public async Task DocumentState_WithProjectWorkspaceStateChange_CachesLoadedText()
    {
        // Arrange
        var state = DocumentState.Create(s_hostDocument, s_textLoader);
        var textAndVersion = await state.GetTextAndVersionAsync(DisposalToken);

        // Act
        var newState = state.WithProjectWorkspaceStateChange();
        var newTextAndVersion = await newState.GetTextAndVersionAsync(DisposalToken);

        // Assert
        Assert.NotSame(state, newState);
        Assert.Same(textAndVersion, newTextAndVersion);
        Assert.True(newState.TryGetText(out _));
        Assert.True(newState.TryGetTextVersion(out _));
    }
}
