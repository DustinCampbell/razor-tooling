// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorEngineFeatureBase : IRazorEngineFeature
{
    private RazorProjectEngine? _projectEngine;

    public RazorProjectEngine ProjectEngine => _projectEngine.AssumeNotNull();

    public void Initialize(RazorProjectEngine projectEngine)
    {
        ArgHelper.ThrowIfNull(projectEngine);

        if (_projectEngine is not null)
        {
            ThrowHelper.ThrowInvalidOperationException($"{nameof(IRazorProjectEngineFeature)} is already initialized.");
        }

        _projectEngine = projectEngine;

        OnInitialized();
    }

    protected virtual void OnInitialized()
    {
    }

    protected TFeature GetRequiredFeature<TFeature>()
        where TFeature : class, IRazorEngineFeature
    {
        if (ProjectEngine == null)
        {
            throw new InvalidOperationException(Resources.FormatFeatureMustBeInitialized(nameof(ProjectEngine)));
        }

        if (ProjectEngine.TryGetFeature(out TFeature? feature))
        {
            return feature;
        }

        throw new InvalidOperationException(
            Resources.FormatFeatureDependencyMissing(
                GetType().Name,
                typeof(TFeature).Name,
                typeof(RazorProjectEngine).Name));
    }

    protected void ThrowForMissingDocumentDependency<TDocumentDependency>(TDocumentDependency value)
    {
        if (value == null)
        {
            throw new InvalidOperationException(
                Resources.FormatFeatureDependencyMissing(
                    GetType().Name,
                    typeof(TDocumentDependency).Name,
                    typeof(RazorCodeDocument).Name));
        }
    }
}
