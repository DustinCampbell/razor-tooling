// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal class ComponentSplatLoweringPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    // Run after component lowering pass
    public override int Order => 50;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        if (!IsComponentDocument(documentNode))
        {
            return;
        }

        var references = documentNode.FindDescendantReferences<TagHelperDirectiveAttributeIntermediateNode>();
        var parents = new HashSet<IntermediateNode>();
        foreach (var reference in references)
        {
            parents.Add(reference.Parent);
        }

        foreach (var reference in references)
        {
            var node = (TagHelperDirectiveAttributeIntermediateNode)reference.Node;
            if (node.TagHelper.IsSplatTagHelper())
            {
                reference.Replace(RewriteUsage(reference.Parent, node));
            }
        }
    }

    private IntermediateNode RewriteUsage(IntermediateNode parent, TagHelperDirectiveAttributeIntermediateNode node)
    {
        var result = new SplatIntermediateNode()
        {
            Source = node.Source,
        };

        result.Children.AddRange(node.FindDescendantNodes<IntermediateToken>().Where(static t => t.IsCSharp));
        result.Diagnostics.AddRange(node.Diagnostics);
        return result;
    }
}
