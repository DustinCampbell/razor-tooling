// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class DefaultRazorCodeGenerationOptionsFactoryProjectFeature : RazorProjectEngineFeatureBase, IRazorCodeGenerationOptionsFactoryProjectFeature
{
    private ImmutableArray<IConfigureRazorCodeGenerationOptionsFeature> _configureOptions;

    protected override void OnInitialized()
    {
        _configureOptions = ProjectEngine.Features.OfType<IConfigureRazorCodeGenerationOptionsFeature>().ToImmutableArray();
    }

    public RazorCodeGenerationOptions Create(string fileKind, Action<RazorCodeGenerationOptionsBuilder> configure)
    {
        var builder = new DefaultRazorCodeGenerationOptionsBuilder(ProjectEngine.Configuration, fileKind);

        configure?.Invoke(builder);

        foreach (var option in _configureOptions)
        {
            option.Configure(builder);
        }

        return builder.Build();
    }
}
