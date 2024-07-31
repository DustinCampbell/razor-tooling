// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorEngine
{
    public ImmutableArray<IRazorEngineFeature> Features { get; }

    internal RazorEngine(ImmutableArray<IRazorEngineFeature> features)
    {
        Features = features;

        foreach (var feature in features)
        {
            feature.Engine = this;
        }
    }

    internal bool TryGetFeature<TFeature>([NotNullWhen(true)] out TFeature? feature)
        where TFeature : class, IRazorEngineFeature
    {
        foreach (var item in Features)
        {
            if (item is TFeature result)
            {
                feature = result;
                return true;
            }
        }

        feature = null;
        return false;
    }
}
