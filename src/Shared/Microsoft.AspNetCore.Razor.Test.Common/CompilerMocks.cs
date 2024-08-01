// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Moq;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal static class CompilerMocks
{
    public static Mock<T> CreateEngineFeatureMock<T>()
        where T : class, IRazorEngineFeature
    {
        var mock = new StrictMock<T>();

        mock.Setup(
            x => x.Initialize(It.IsAny<RazorProjectEngine>()),
            out RazorProjectEngine engine);

        mock.SetupGet(m => m.Engine)
            .Returns(() => engine);

        return mock;
    }
}
