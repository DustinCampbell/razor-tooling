﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

#pragma warning disable CS0618 // Type or member is obsolete
internal class DefaultRazorParserOptionsFeature : RazorEngineFeatureBase, IRazorParserOptionsFeature
#pragma warning restore CS0618 // Type or member is obsolete
{
    private readonly bool _designTime;
    private readonly RazorLanguageVersion _version;
    private readonly string? _fileKind;
    private ImmutableArray<IConfigureRazorParserOptionsFeature> _configureOptions;

    public DefaultRazorParserOptionsFeature(bool designTime, RazorLanguageVersion version, string? fileKind)
    {
        _designTime = designTime;
        _version = version;
        _fileKind = fileKind;
    }

    protected override void OnInitialized()
    {
        _configureOptions = Engine.GetFeatures<IConfigureRazorParserOptionsFeature>();
    }

    public RazorParserOptions GetOptions()
    {
        var builder = new RazorParserOptionsBuilder(_designTime, _version, _fileKind);

        foreach (var options in _configureOptions)
        {
            options.Configure(builder);
        }

        return builder.Build();
    }
}
