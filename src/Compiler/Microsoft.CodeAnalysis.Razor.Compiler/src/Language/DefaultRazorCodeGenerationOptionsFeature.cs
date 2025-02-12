// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

#pragma warning disable CS0618 // Type or member is obsolete
internal class DefaultRazorCodeGenerationOptionsFeature(RazorLanguageVersion languageVersion) : RazorEngineFeatureBase, IRazorCodeGenerationOptionsFeature
#pragma warning restore CS0618 // Type or member is obsolete
{
    private readonly RazorLanguageVersion _languageVersion = languageVersion;
    private ImmutableArray<IConfigureCodeGenerationOptionsFeature> _features;

    protected override void OnInitialized()
    {
        _features = Engine.GetFeatures<IConfigureCodeGenerationOptionsFeature>().OrderByAsArray(static x => x.Order);
    }

    public RazorCodeGenerationOptions GetOptions()
    {
        var builder = new RazorCodeGenerationOptions.Builder(_languageVersion);

        foreach (var feature in _features)
        {
            feature.Configure(builder);
        }

        return builder.ToOptions();
    }
}
