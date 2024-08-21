// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer;

public class DocumentSnapshotTextLoaderTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public async Task LoadTextAndVersionAsync_CreatesTextAndVersionFromDocumentsText()
    {
        // Arrange
        var expectedSourceText = SourceText.From("Hello World");

        var snapshot = TestMocks.CreateDocumentSnapshot(b =>
        {
            b.SetupText(expectedSourceText);
        });

        var textLoader = new DocumentSnapshotTextLoader(snapshot);

        // Act
        var actual = await textLoader.LoadTextAndVersionAsync(default, default);

        // Assert
        Assert.Same(expectedSourceText, actual.Text);
    }
}
