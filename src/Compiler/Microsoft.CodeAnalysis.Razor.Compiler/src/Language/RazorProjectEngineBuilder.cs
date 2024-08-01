// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorProjectEngineBuilder
{
    public RazorConfiguration Configuration { get; }
    public RazorProjectFileSystem FileSystem { get; }
    public ImmutableArray<IRazorEngineFeature>.Builder Features { get; }
    public ImmutableArray<IRazorEnginePhase>.Builder Phases { get; }

    internal RazorProjectEngineBuilder(RazorConfiguration configuration, RazorProjectFileSystem fileSystem)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        Features = ImmutableArray.CreateBuilder<IRazorEngineFeature>();
        Phases = ImmutableArray.CreateBuilder<IRazorEnginePhase>();
    }

    public RazorProjectEngine Build()
    {
        return new RazorProjectEngine(
            Configuration,
            FileSystem,
            Features.DrainToImmutable(),
            Phases.DrainToImmutable());
    }
}
