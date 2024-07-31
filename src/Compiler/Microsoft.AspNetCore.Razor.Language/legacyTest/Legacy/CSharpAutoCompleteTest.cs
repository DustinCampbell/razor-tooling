// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Razor.Language.Extensions;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class CSharpAutoCompleteTest() : ParserTestBase(layer: TestProject.Layer.Compiler, validateSpanEditHandlers: true)
{
    [Fact]
    public void FunctionsDirectiveAutoCompleteAtEOF()
    {
        // Arrange, Act & Assert
        ParseDocumentTest("@functions{", [FunctionsDirective.Descriptor]);
    }

    [Fact]
    public void SectionDirectiveAutoCompleteAtEOF()
    {
        // Arrange, Act & Assert
        ParseDocumentTest("@section Header {", [SectionDirective.Descriptor]);
    }

    [Fact]
    public void VerbatimBlockAutoCompleteAtEOF()
    {
        ParseDocumentTest("@{");
    }

    [Fact]
    public void FunctionsDirectiveAutoCompleteAtStartOfFile()
    {
        // Arrange, Act & Assert
        ParseDocumentTest("""
            @functions{
            foo
            """, [FunctionsDirective.Descriptor]);
    }

    [Fact]
    public void SectionDirectiveAutoCompleteAtStartOfFile()
    {
        // Arrange, Act & Assert
        ParseDocumentTest("""
            @section Header {
            <p>Foo</p>
            """, [SectionDirective.Descriptor]);
    }

    [Fact]
    public void VerbatimBlockAutoCompleteAtStartOfFile()
    {
        ParseDocumentTest("""
            @{
            <p></p>
            """);
    }
}
