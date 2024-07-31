// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

#pragma warning disable CS0618 // Type or member is obsolete
internal class DefaultRazorParserOptionsFeature(bool designTime, RazorLanguageVersion version, string? fileKind) : RazorEngineFeatureBase, IRazorParserOptionsFeature
#pragma warning restore CS0618 // Type or member is obsolete
{
    private readonly bool _designTime = designTime;
    private readonly RazorLanguageVersion _version = version;
    private readonly string? _fileKind = fileKind;
    private ImmutableArray<IConfigureRazorParserOptionsFeature> _configureOptions;

    protected override void OnInitialized()
    {
        _configureOptions = Engine.Features.OfType<IConfigureRazorParserOptionsFeature>().ToImmutableArray();
    }

    public RazorParserOptions GetOptions()
    {
        var builder = new RazorParserOptionsBuilder(_designTime, _version, _fileKind);

        foreach (var option in _configureOptions)
        {
            option.Configure(builder);
        }

        return builder.Build();
    }
}
