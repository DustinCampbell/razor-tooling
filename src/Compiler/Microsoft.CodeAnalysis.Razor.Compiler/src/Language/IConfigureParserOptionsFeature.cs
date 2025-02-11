// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

internal interface IConfigureParserOptionsFeature : IRazorEngineFeature
{
    int Order { get; }

    void Configure(RazorParserOptions.Builder builder);
}
