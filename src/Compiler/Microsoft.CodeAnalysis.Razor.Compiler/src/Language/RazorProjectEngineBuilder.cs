// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorProjectEngineBuilder
{
    public RazorConfiguration Configuration { get; }
    public RazorProjectFileSystem FileSystem { get; }
    public ImmutableArray<IRazorEngineFeature>.Builder Features { get; }
    public ImmutableArray<IRazorEnginePhase>.Builder Phases { get; }

    private RazorTargetExtensionFeatureBuilder? _targetExtensionFeatureBuilder;
    private ImmutableArray<ICodeTargetExtension>.Builder? _targetExtensions;

    internal RazorProjectEngineBuilder(RazorConfiguration configuration, RazorProjectFileSystem fileSystem)
    {
        ArgHelper.ThrowIfNull(configuration);
        ArgHelper.ThrowIfNull(fileSystem);

        Configuration = configuration;
        FileSystem = fileSystem;

        Features = ImmutableArray.CreateBuilder<IRazorEngineFeature>();
        Phases = ImmutableArray.CreateBuilder<IRazorEnginePhase>();
    }

    /// <summary>
    /// Adds the specified <see cref="ICodeTargetExtension"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
    /// <param name="extension">The <see cref="ICodeTargetExtension"/> to add.</param>
    /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
    public RazorProjectEngineBuilder AddTargetExtension(ICodeTargetExtension extension)
    {
        ArgHelper.ThrowIfNull(extension);

        if (_targetExtensions is null)
        {
            _targetExtensions = ImmutableArray.CreateBuilder<ICodeTargetExtension>();
            _targetExtensionFeatureBuilder = new RazorTargetExtensionFeatureBuilder(_targetExtensions);
            Features.Add(_targetExtensionFeatureBuilder);
        }

        _targetExtensions.Add(extension);

        return this;
    }

    private sealed class RazorTargetExtensionFeatureBuilder : IRazorTargetExtensionFeature
    {
        private readonly ImmutableArray<ICodeTargetExtension>.Builder _targetExtensions;

        public RazorTargetExtensionFeatureBuilder(ImmutableArray<ICodeTargetExtension>.Builder targetExtensions)
        {
            _targetExtensions = targetExtensions;
        }

        public ImmutableArray<ICodeTargetExtension> TargetExtensions
            => throw new NotImplementedException();

        public RazorProjectEngine Engine => throw new NotImplementedException();

        public void Initialize(RazorProjectEngine projectEngine)
            => throw new NotImplementedException();
    }

    public RazorProjectEngine Build()
    {
        if (_targetExtensionFeatureBuilder is { } featureBuilder)
        {
            var found = false;

            for (var i = 0; i < Features.Count; i++)
            {
                var feature = Features[i];
                if (feature == featureBuilder)
                {
                    Features[i] = new DefaultRazorTargetExtensionFeature(_targetExtensions!.DrainToImmutable());
                    found = true;
                    break;
                }
            }

            Debug.Assert(found);
        }

        return new RazorProjectEngine(
            Configuration,
            FileSystem,
            Features.DrainToImmutable(),
            Phases.DrainToImmutable());
    }
}
