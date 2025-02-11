// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

#pragma warning disable CS0618 // Type or member is obsolete
internal class DefaultRazorParserOptionsFeature(RazorLanguageVersion version) : RazorEngineFeatureBase, IRazorParserOptionsFeature
#pragma warning restore CS0618 // Type or member is obsolete
{
    private readonly RazorLanguageVersion _version = version;
    private ImmutableArray<IConfigureRazorParserOptionsFeature> _configureOptions;

    protected override void OnInitialized()
    {
        _configureOptions = Engine.GetFeatures<IConfigureRazorParserOptionsFeature>();
    }

    public RazorParserOptions GetOptions()
    {
        var builder = new RazorParserOptions.Builder(_version, FileKinds.Legacy);

        foreach (var options in _configureOptions)
        {
            options.Configure(builder);
        }

        return builder.ToOptions();
    }
}
