// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Serialization;
using Newtonsoft.Json;
using static Microsoft.AspNetCore.Razor.Language.DefaultRazorTagHelperContextDiscoveryPhase;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

public class RazorTagHelperParsingBenchmark
{
    public RazorTagHelperParsingBenchmark()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "taghelpers.json")))
        {
            current = current.Parent;
        }

        var root = current;

        var tagHelpers = ReadTagHelpers(Path.Combine(root.FullName, "taghelpers.json"));
        var tagHelperFeature = new StaticTagHelperFeature(tagHelpers);

        var blazorServerTagHelpersFilePath = Path.Combine(root.FullName, "BlazorServerTagHelpers.razor");

        var fileSystem = RazorProjectFileSystem.Create(root.FullName);
        ProjectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem,
            b =>
            {
                RazorExtensions.Register(b);
                b.Features.Add(tagHelperFeature);
            });
        BlazorServerTagHelpersDemoFile = fileSystem.GetItem(Path.Combine(blazorServerTagHelpersFilePath), FileKinds.Component);

        var matches = new HashSet<TagHelperDescriptor>();
        ComponentDirectiveVisitor = new ComponentDirectiveVisitor(blazorServerTagHelpersFilePath, tagHelpers, currentNamespace: null, matches);
        var codeDocument = ProjectEngine.ProcessDesignTime(BlazorServerTagHelpersDemoFile);
        SyntaxTree = codeDocument.GetSyntaxTree();
    }

    private RazorProjectEngine ProjectEngine { get; }
    private RazorProjectItem BlazorServerTagHelpersDemoFile { get; }
    private ComponentDirectiveVisitor ComponentDirectiveVisitor { get; }
    private RazorSyntaxTree SyntaxTree { get; }

    [Benchmark(Description = "TagHelper Design Time Processing")]
    public void TagHelper_ProcessDesignTime()
    {
        _ = ProjectEngine.ProcessDesignTime(BlazorServerTagHelpersDemoFile);
    }

    [Benchmark(Description = "Component Directive Parsing")]
    public void TagHelper_ComponentDirectiveVisitor()
    {
        ComponentDirectiveVisitor.Visit(SyntaxTree);
    }

    private static TagHelperDescriptorCollection ReadTagHelpers(string filePath)
    {
        var serializer = new JsonSerializer();
        serializer.Converters.Add(new RazorDiagnosticJsonConverter());
        serializer.Converters.Add(new TagHelperDescriptorJsonConverter());

        using var reader = new JsonTextReader(File.OpenText(filePath));

        var tagHelpers = serializer.Deserialize<TagHelperDescriptor[]>(reader);
        return TagHelperDescriptorCollection.Create(tagHelpers);
    }

    private sealed class StaticTagHelperFeature(TagHelperDescriptorCollection descriptors) : RazorEngineFeatureBase, ITagHelperFeature
    {
        public TagHelperDescriptorCollection Descriptors { get; } = descriptors;

        public TagHelperDescriptorCollection GetDescriptors() => Descriptors;
    }
}
