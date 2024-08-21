// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Editor;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Razor.Debugging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor;

public class RazorLanguageService_IVsLanguageDebugInfoTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private readonly TextSpan[] _textSpans = [new TextSpan()];

    [Fact]
    public void ValidateBreakpointLocation_CanNotGetBackingTextBuffer_ReturnsNotImpl()
    {
        // Arrange
        var editorAdaptersFactoryService = new StrictMock<IVsEditorAdaptersFactoryService>();
        editorAdaptersFactoryService
            .Setup(s => s.GetDataBuffer(It.IsAny<IVsTextBuffer>()))
            .ReturnsNull();
        var languageService = CreateLanguageServiceWith(editorAdaptersFactory: editorAdaptersFactoryService.Object);

        // Act
        var result = languageService.ValidateBreakpointLocation(StrictMock.Of<IVsTextBuffer>(), iLine: 0, iCol: 0, _textSpans);

        // Assert
        Assert.Equal(VSConstants.E_NOTIMPL, result);
    }

    [Fact]
    public void ValidateBreakpointLocation_InvalidLocation_ReturnsEFail()
    {
        // Arrange
        var languageService = CreateLanguageServiceWith();

        // Act
        var result = languageService.ValidateBreakpointLocation(StrictMock.Of<IVsTextBuffer>(), iLine: int.MaxValue, iCol: 0, _textSpans);

        // Assert
        Assert.Equal(VSConstants.E_FAIL, result);
    }

    [Fact]
    public void ValidateBreakpointLocation_NullBreakpointRange_ReturnsEFail()
    {
        // Arrange
        var languageService = CreateLanguageServiceWith();

        // Act
        var result = languageService.ValidateBreakpointLocation(StrictMock.Of<IVsTextBuffer>(), iLine: 0, iCol: 0, _textSpans);

        // Assert
        Assert.Equal(VSConstants.E_FAIL, result);
    }

    [Fact]
    public void ValidateBreakpointLocation_ValidBreakpointRange_ReturnsSOK()
    {
        // Arrange
        var breakpointRange = VsLspFactory.CreateRange(2, 4, 3, 5);
        var resolverMock = new StrictMock<RazorBreakpointResolver>();
        resolverMock
            .Setup(x => x.TryResolveBreakpointRangeAsync(It.IsAny<ITextBuffer>(), /*lineIndex*/ 0, /*characterIndex*/ 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(breakpointRange);

        var languageService = CreateLanguageServiceWith(resolverMock.Object);

        // Act
        var result = languageService.ValidateBreakpointLocation(StrictMock.Of<IVsTextBuffer>(), 0, 0, _textSpans);

        // Assert
        Assert.Equal(VSConstants.S_OK, result);
        var span = Assert.Single(_textSpans);
        Assert.Equal(2, span.iStartLine);
        Assert.Equal(4, span.iStartIndex);
        Assert.Equal(3, span.iEndLine);
        Assert.Equal(5, span.iEndIndex);
    }

    [Fact]
    public void ValidateBreakpointLocation_CanNotCreateDialog_ReturnsEFail()
    {
        // Arrange
        var uiThreadExecutor = new StrictMock<IUIThreadOperationExecutor>();
        uiThreadExecutor
            .Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<Action<IUIThreadOperationContext>>()))
            .Returns(UIThreadOperationStatus.Canceled);
        var languageService = CreateLanguageServiceWith(uiThreadOperationExecutor: uiThreadExecutor.Object);

        // Act
        var result = languageService.ValidateBreakpointLocation(Mock.Of<IVsTextBuffer>(MockBehavior.Strict), 0, 0, _textSpans);

        // Assert
        Assert.Equal(VSConstants.E_FAIL, result);
    }

    [Fact]
    public void GetProximityExpressions_CanNotGetBackingTextBuffer_ReturnsNotImpl()
    {
        // Arrange
        var editorAdaptersFactoryService = new StrictMock<IVsEditorAdaptersFactoryService>();
        editorAdaptersFactoryService
            .Setup(s => s.GetDataBuffer(It.IsAny<IVsTextBuffer>()))
            .ReturnsNull();
        var languageService = CreateLanguageServiceWith(editorAdaptersFactory: editorAdaptersFactoryService.Object);

        // Act
        var result = languageService.GetProximityExpressions(StrictMock.Of<IVsTextBuffer>(), 0, 0, 0, out _);

        // Assert
        Assert.Equal(VSConstants.E_NOTIMPL, result);
    }

    [Fact]
    public void GetProximityExpressions_InvalidLocation_ReturnsEFail()
    {
        // Arrange
        var languageService = CreateLanguageServiceWith();

        // Act
        var result = languageService.GetProximityExpressions(StrictMock.Of<IVsTextBuffer>(), int.MaxValue, 0, 0, out _);

        // Assert
        Assert.Equal(VSConstants.E_FAIL, result);
    }

    [Fact]
    public void GetProximityExpressions_NullRange_ReturnsEFail()
    {
        // Arrange
        var languageService = CreateLanguageServiceWith();

        // Act
        var result = languageService.GetProximityExpressions(StrictMock.Of<IVsTextBuffer>(), 0, 0, 0, out _);

        // Assert
        Assert.Equal(VSConstants.E_FAIL, result);
    }

    [Fact]
    public void GetProximityExpressions_ValidRange_ReturnsSOK()
    {
        // Arrange
        IReadOnlyList<string> expressions = ["something"];

        var resolverMock = new StrictMock<RazorProximityExpressionResolver>();
        resolverMock
            .Setup(x => x.TryResolveProximityExpressionsAsync(It.IsAny<ITextBuffer>(), /*lineIndex*/ 0, /*characterIndex*/ 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expressions);

        var languageService = CreateLanguageServiceWith(proximityExpressionResolver: resolverMock.Object);

        // Act
        var result = languageService.GetProximityExpressions(Mock.Of<IVsTextBuffer>(MockBehavior.Strict), 0, 0, 0, out var resolvedExpressions);

        // Assert
        Assert.Equal(VSConstants.S_OK, result);
        var concreteResolvedExpressions = Assert.IsType<VsEnumBSTR>(resolvedExpressions);
        Assert.Equal(expressions, concreteResolvedExpressions.Values);
    }

    [Fact]
    public void GetProximityExpressions_CanNotCreateDialog_ReturnsEFail()
    {
        // Arrange
        var uiThreadOperationExecutor = new StrictMock<IUIThreadOperationExecutor>();
        uiThreadOperationExecutor
            .Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<Action<IUIThreadOperationContext>>()))
            .Returns(UIThreadOperationStatus.Canceled);
        var languageService = CreateLanguageServiceWith(uiThreadOperationExecutor: uiThreadOperationExecutor.Object);

        // Act
        var result = languageService.GetProximityExpressions(StrictMock.Of<IVsTextBuffer>(), iLine: 0, iCol: 0, cLines: 0, ppEnum: out _);

        // Assert
        Assert.Equal(VSConstants.E_FAIL, result);
    }

    private RazorLanguageService CreateLanguageServiceWith(
        RazorBreakpointResolver breakpointResolver = null,
        RazorProximityExpressionResolver proximityExpressionResolver = null,
        IUIThreadOperationExecutor uiThreadOperationExecutor = null,
        IVsEditorAdaptersFactoryService editorAdaptersFactory = null)
    {
        if (breakpointResolver is null)
        {
            var mock = new StrictMock<RazorBreakpointResolver>();
            mock.Setup(x => x.TryResolveBreakpointRangeAsync(It.IsAny<ITextBuffer>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(value: null);

            breakpointResolver = mock.Object;
        }

        if (proximityExpressionResolver is null)
        {
            var mock = new StrictMock<RazorProximityExpressionResolver>();
            mock.Setup(x => x.TryResolveProximityExpressionsAsync(It.IsAny<ITextBuffer>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(value: null);

            proximityExpressionResolver = mock.Object;
        }

        uiThreadOperationExecutor ??= new TestIUIThreadOperationExecutor();

        if (editorAdaptersFactory is null)
        {
            var mock = new StrictMock<IVsEditorAdaptersFactoryService>();
            mock.Setup(x => x.GetDataBuffer(It.IsAny<IVsTextBuffer>()))
                .Returns(new TestTextBuffer(new StringTextSnapshot(Environment.NewLine), contentType: null));

            editorAdaptersFactory = mock.Object;
        }

        var lspServerActivationTracker = new LspServerActivationTracker();
        lspServerActivationTracker.Activated();

        var languageService = new RazorLanguageService(breakpointResolver, proximityExpressionResolver, lspServerActivationTracker, uiThreadOperationExecutor, editorAdaptersFactory, JoinableTaskFactory);
        return languageService;
    }

    private class TestIUIThreadOperationExecutor : IUIThreadOperationExecutor
    {
        public IUIThreadOperationContext BeginExecute(string title, string defaultDescription, bool allowCancellation, bool showProgress)
        {
            throw new NotImplementedException();
        }

        public IUIThreadOperationContext BeginExecute(UIThreadOperationExecutionOptions executionOptions)
        {
            throw new NotImplementedException();
        }

        public UIThreadOperationStatus Execute(string title, string defaultDescription, bool allowCancellation, bool showProgress, Action<IUIThreadOperationContext> action)
        {
            using (var context = new TestUIThreadOperationContext())
            {
                action(context);
            }

            return UIThreadOperationStatus.Completed;
        }

        public UIThreadOperationStatus Execute(UIThreadOperationExecutionOptions executionOptions, Action<IUIThreadOperationContext> action)
        {
            throw new NotImplementedException();
        }

        private class TestUIThreadOperationContext : IUIThreadOperationContext
        {
            public TestUIThreadOperationContext()
            {
            }

            public CancellationToken UserCancellationToken => new();

            public bool AllowCancellation => throw new NotImplementedException();

            public string Description => throw new NotImplementedException();

            public IEnumerable<IUIThreadOperationScope> Scopes => throw new NotImplementedException();

            public PropertyCollection Properties => throw new NotImplementedException();

            public IUIThreadOperationScope AddScope(bool allowCancellation, string description)
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
            }

            public void TakeOwnership()
            {
                throw new NotImplementedException();
            }
        }
    }
}
