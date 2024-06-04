﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.VisualStudio.Razor.Logging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor;

public class VisualStudioLSPEditorFeatureDetectorTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [UIFact]
    public void IsLSPEditorAvailable_LegacyEditorDisabled_ReturnsTrue()
    {
        // Arrange
        using var activityLog = GetRazorActivityLog();
        var featureDetector = new TestLSPEditorFeatureDetector(activityLog)
        {
            UseLegacyEditor = false,
        };

        // Act
        var result = featureDetector.IsLSPEditorAvailable();

        // Assert
        Assert.True(result);
    }

    [UIFact]
    public void IsLSPEditorAvailable_LegacyEditorEnabled_ReturnsFalse()
    {
        // Arrange
        using var activityLog = GetRazorActivityLog();
        var featureDetector = new TestLSPEditorFeatureDetector(activityLog)
        {
            UseLegacyEditor = true,
        };

        // Act
        var result = featureDetector.IsLSPEditorAvailable();

        // Assert
        Assert.False(result);
    }

    [UIFact]
    public void IsLSPEditorAvailable_IsVSRemoteClient_ReturnsTrue()
    {
        // Arrange
        using var activityLog = GetRazorActivityLog();
        var featureDetector = new TestLSPEditorFeatureDetector(activityLog)
        {
            IsVSRemoteClientValue = true,
        };

        // Act
        var result = featureDetector.IsLSPEditorAvailable();

        // Assert
        Assert.True(result);
    }

    [UIFact]
    public void IsRemoteClient_IsRemoteClient_ReturnsTrue()
    {
        // Arrange
        using var activityLog = GetRazorActivityLog();
        var featureDetector = new TestLSPEditorFeatureDetector(activityLog)
        {
            IsVSRemoteClientValue = true,
        };

        // Act
        var result = featureDetector.IsRemoteClient();

        // Assert
        Assert.True(result);
    }

    [UIFact]
    public void IsRemoteClient_LiveShareGuest_ReturnsTrue()
    {
        // Arrange
        using var activityLog = GetRazorActivityLog();
        var featureDetector = new TestLSPEditorFeatureDetector(activityLog)
        {
            IsLiveShareGuestValue = true,
        };

        // Act
        var result = featureDetector.IsRemoteClient();

        // Assert
        Assert.True(result);
    }

    [UIFact]
    public void IsRemoteClient_UnknownEnvironment_ReturnsFalse()
    {
        // Arrange
        using var activityLog = GetRazorActivityLog();
        var featureDetector = new TestLSPEditorFeatureDetector(activityLog);

        // Act
        var result = featureDetector.IsRemoteClient();

        // Assert
        Assert.False(result);
    }

    private RazorActivityLog GetRazorActivityLog()
    {
        var vsActivityLogMock = new StrictMock<IVsActivityLog>();
        vsActivityLogMock
            .Setup(x => x.LogEntry(It.IsAny<uint>(), "Razor", It.IsAny<string>()))
            .Returns(VSConstants.S_OK);

        var serviceProviderMock = new StrictMock<IAsyncServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetServiceAsync(typeof(SVsActivityLog)))
            .ReturnsAsync(vsActivityLogMock.Object);

        return new RazorActivityLog(serviceProviderMock.Object, JoinableTaskContext);
    }

    private class TestLSPEditorFeatureDetector(RazorActivityLog activityLog)
        : VisualStudioLSPEditorFeatureDetector(activityLog)
    {
        public bool UseLegacyEditor { get; set; }

        public bool IsLiveShareGuestValue { get; set; }

        public bool IsLiveShareHostValue { get; set; }

        public bool IsVSRemoteClientValue { get; set; }

        public override bool IsLSPEditorAvailable() => !UseLegacyEditor;

        public override bool IsLiveShareHost() => IsLiveShareHostValue;

        private protected override bool IsLiveShareGuest() => IsLiveShareGuestValue;

        private protected override bool IsVSRemoteClient() => IsVSRemoteClientValue;
    }
}
