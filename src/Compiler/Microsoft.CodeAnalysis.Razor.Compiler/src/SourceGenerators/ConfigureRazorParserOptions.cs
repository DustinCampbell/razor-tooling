// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators;

internal class ConfigureRazorParserOptions(bool useRoslynTokenizer, CSharpParseOptions csharpParseOptions) : RazorEngineFeatureBase, IConfigureRazorParserOptionsFeature
{
    public int Order { get; set; }

    public void Configure(RazorParserOptions.Builder builder)
    {
        builder.UseRoslynTokenizer = useRoslynTokenizer;
        builder.CSharpParseOptions = csharpParseOptions;
    }
}
