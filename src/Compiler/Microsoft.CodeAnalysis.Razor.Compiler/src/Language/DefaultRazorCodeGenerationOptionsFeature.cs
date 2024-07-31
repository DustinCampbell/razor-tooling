// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

#pragma warning disable CS0618 // Type or member is obsolete
internal class DefaultRazorCodeGenerationOptionsFeature(bool designTime) : RazorEngineFeatureBase, IRazorCodeGenerationOptionsFeature
#pragma warning restore CS0618 // Type or member is obsolete
{
    private readonly bool _designTime = designTime;
    private ImmutableArray<IConfigureRazorCodeGenerationOptionsFeature> _configureOptions;

    protected override void OnInitialized()
    {
        _configureOptions = Engine.Features.OfType<IConfigureRazorCodeGenerationOptionsFeature>().ToImmutableArray();
    }

    public RazorCodeGenerationOptions GetOptions()
        => _designTime
            ? RazorCodeGenerationOptions.CreateDesignTime(ConfigureOptions)
            : RazorCodeGenerationOptions.Create(ConfigureOptions);

    private void ConfigureOptions(RazorCodeGenerationOptionsBuilder builder)
    {
        foreach (var option in _configureOptions)
        {
            option.Configure(builder);
        }
    }
}
