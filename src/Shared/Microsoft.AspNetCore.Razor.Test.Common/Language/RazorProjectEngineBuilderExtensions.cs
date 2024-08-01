// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language;

public static class RazorProjectEngineBuilderExtensions
{
    public static RazorProjectEngineBuilder AddTagHelpers(this RazorProjectEngineBuilder builder, params TagHelperDescriptor[] tagHelpers)
    {
        return AddTagHelpers(builder, (IEnumerable<TagHelperDescriptor>)tagHelpers);
    }

    public static RazorProjectEngineBuilder AddTagHelpers(this RazorProjectEngineBuilder builder, IEnumerable<TagHelperDescriptor> tagHelpers)
    {
        var feature = (TestTagHelperFeature)builder.Features.OfType<ITagHelperFeature>().FirstOrDefault();
        if (feature == null)
        {
            feature = new TestTagHelperFeature();
            builder.Features.Add(feature);
        }

        feature.TagHelpers.AddRange(tagHelpers);
        return builder;
    }

    public static RazorProjectEngineBuilder ConfigureDocumentClassifier(this RazorProjectEngineBuilder builder, string testFileName)
    {
        var features = builder.Features;
        var featureIndex = -1;

        for (var i = 0; i < features.Count; i++)
        {
            if (features[i] is DefaultDocumentClassifierPassFeature)
            {
                featureIndex = i;
                break;
            }
        }

        var configureClass = (RazorCodeDocument codeDocument, ClassDeclarationIntermediateNode node) =>
        {
            node.ClassName = testFileName.Replace('/', '_');
            node.Modifiers.Clear();
            node.Modifiers.Add("public");
        };

        var configureNamespace = (RazorCodeDocument codeDocument, NamespaceDeclarationIntermediateNode node) =>
        {
            node.Content = "Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles";
        };

        var configureMethod = (RazorCodeDocument codeDocument, MethodDeclarationIntermediateNode node) =>
        {
            node.Modifiers.Clear();
            node.Modifiers.Add("public");
            node.Modifiers.Add("async");
            node.MethodName = "ExecuteAsync";
            node.ReturnType = typeof(Task).FullName;
        };

        var feature = new DefaultDocumentClassifierPassFeature([configureClass], [configureNamespace], [configureMethod]);

        if (featureIndex >= 0)
        {
            builder.Features[featureIndex] = feature;
        }
        else
        {
            builder.Features.Add(feature);
        }

        return builder;
    }
}
