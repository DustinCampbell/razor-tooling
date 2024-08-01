// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class RazorParserOptionsFactory : RazorEngineFeatureBase, IRazorParserOptionsFactory
{
    private ImmutableArray<IConfigureRazorParserOptionsFeature> _configureOptions;

    protected override void OnInitialized()
    {
        _configureOptions = Engine.Features.OfType<IConfigureRazorParserOptionsFeature>().ToImmutableArray();
    }

    public RazorParserOptions Create(string? fileKind = null, Action<RazorParserOptionsBuilder>? configure = null)
    {
        var builder = new RazorParserOptionsBuilder(Engine.Configuration, fileKind);

        configure?.Invoke(builder);

        foreach (var option in _configureOptions)
        {
            option.Configure(builder);
        }

        return builder.Build();
    }
}
