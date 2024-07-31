// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorProjectEngineFeatureBase : IRazorProjectEngineFeature
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
}
