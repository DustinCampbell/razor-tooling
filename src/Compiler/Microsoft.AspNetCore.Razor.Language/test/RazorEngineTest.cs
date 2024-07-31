// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class RazorEngineTest
{
    [Fact]
    public void Ctor_InitializesPhasesAndFeatures()
    {
        // Arrange
        var features = ImmutableArray.Create(
            Mock.Of<IRazorEngineFeature>(),
            Mock.Of<IRazorEngineFeature>());

        // Act
        var engine = new RazorEngine(features);

        // Assert
        foreach (var feature in features)
        {
            Assert.Same(engine, feature.Engine);
        }
    }
}
