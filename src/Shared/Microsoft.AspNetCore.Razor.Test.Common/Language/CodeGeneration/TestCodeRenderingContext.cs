// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public sealed class TestCodeRenderingContext : CodeRenderingContext
{
    private readonly string _uniqueId;

    private TestCodeRenderingContext(
        IntermediateNodeWriter nodeWriter,
        RazorSourceDocument sourceDocument,
        DocumentIntermediateNode documentNode,
        RazorCodeGenerationOptions options,
        string uniqueId)
        : base(nodeWriter, sourceDocument, documentNode, options)
    {
        _uniqueId = uniqueId;
    }

    public override string GetDeterministicId()
        => _uniqueId;

    public static CodeRenderingContext CreateDesignTime(
        string newLineString = null,
        string uniqueId = "test",
        RazorSourceDocument source = null,
        IntermediateNodeWriter nodeWriter = null)
    {
        nodeWriter ??= new RuntimeNodeWriter();
        source ??= TestRazorSourceDocument.Create();
        var documentNode = new DocumentIntermediateNode();

        var options = ConfigureOptions(RazorCodeGenerationOptions.DesignTimeDefault, newLineString);

        var context = new TestCodeRenderingContext(nodeWriter, source, documentNode, options, uniqueId);
        context.SetVisitor(new RenderChildrenVisitor(context.CodeWriter));

        return context;
    }

    public static CodeRenderingContext CreateRuntime(
        string newLineString = null,
        string uniqueId = "test",
        RazorSourceDocument source = null,
        IntermediateNodeWriter nodeWriter = null)
    {
        nodeWriter ??= new RuntimeNodeWriter();
        source ??= TestRazorSourceDocument.Create();
        var documentNode = new DocumentIntermediateNode();

        var options = ConfigureOptions(RazorCodeGenerationOptions.Default, newLineString);

        var context = new TestCodeRenderingContext(nodeWriter, source, documentNode, options, uniqueId);
        context.SetVisitor(new RenderChildrenVisitor(context.CodeWriter));

        return context;
    }

    private static RazorCodeGenerationOptions ConfigureOptions(RazorCodeGenerationOptions options, string newLine)
    {
        return newLine is not null
            ? options.WithNewLine(newLine)
            : options;
    }

    private class RenderChildrenVisitor(CodeWriter writer) : IntermediateNodeVisitor
    {
        public override void VisitDefault(IntermediateNode node)
        {
            writer.WriteLine("Render Children");
        }
    }
}
