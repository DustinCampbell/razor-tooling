// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorEnginePhaseBase : IRazorEnginePhase
{
    private RazorProjectEngine? _projectEngine;

    public RazorProjectEngine ProjectEngine => _projectEngine.AssumeNotNull();

    public void Initialize(RazorProjectEngine projectEngine)
    {
        ArgHelper.ThrowIfNull(projectEngine);

        if (_projectEngine is not null)
        {
            ThrowHelper.ThrowInvalidOperationException($"{nameof(IRazorEngineFeature)} is already initialized.");
        }

        _projectEngine = projectEngine;

        OnInitialized();
    }

    protected virtual void OnInitialized()
    {
    }

    public void Execute(RazorCodeDocument codeDocument)
    {
        ArgHelper.ThrowIfNull(codeDocument);

        if (ProjectEngine == null)
        {
            ThrowHelper.ThrowInvalidOperationException(Resources.FormatPhaseMustBeInitialized(nameof(ProjectEngine)));
        }

        ExecuteCore(codeDocument);
    }

    protected abstract void ExecuteCore(RazorCodeDocument codeDocument);

    protected T GetRequiredFeature<T>()
        where T : class
    {
        if (ProjectEngine == null)
        {
            throw new InvalidOperationException(Resources.FormatFeatureMustBeInitialized(nameof(ProjectEngine)));
        }

        var feature = ProjectEngine.Features.OfType<T>().FirstOrDefault();
        ThrowForMissingFeatureDependency(feature);

        return feature;
    }

    protected void ThrowForMissingDocumentDependency<TDocumentDependency>(TDocumentDependency value)
    {
        if (value == null)
        {
            throw new InvalidOperationException(
                Resources.FormatPhaseDependencyMissing(
                    GetType().Name,
                    typeof(TDocumentDependency).Name,
                    typeof(RazorCodeDocument).Name));
        }
    }

    protected void ThrowForMissingFeatureDependency<TEngineDependency>([NotNull] TEngineDependency? value)
        where TEngineDependency : class
    {
        if (value == null)
        {
            throw new InvalidOperationException(
                Resources.FormatPhaseDependencyMissing(
                    GetType().Name,
                    typeof(TEngineDependency).Name,
                    typeof(RazorProjectEngine).Name));
        }
    }
}
