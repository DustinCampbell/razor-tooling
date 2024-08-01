// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        ArgHelper.ThrowIfNull(configuration);
        ArgHelper.ThrowIfNull(fileSystem);

        Configuration = configuration;
        FileSystem = fileSystem;

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
