// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;

namespace Microsoft.AspNetCore.Razor.Test.Common.ProjectSystem;

internal class TestProjectEngineFactoryProvider(Action<RazorProjectEngineBuilder>? configure = null) : IProjectEngineFactoryProvider
{
    private readonly Action<RazorProjectEngineBuilder>? _configure = configure;

    public IProjectEngineFactory GetFactory(RazorConfiguration configuration)
        => new FactoryWrapper(ProjectEngineFactories.DefaultProvider.GetFactory(configuration), _configure);

    private sealed class FactoryWrapper(IProjectEngineFactory factory, Action<RazorProjectEngineBuilder>? outerConfigure) : IProjectEngineFactory
    {
        public string ConfigurationName => factory.ConfigurationName;

        public RazorProjectEngine Create(
            RazorConfiguration configuration,
            RazorProjectFileSystem fileSystem,
            Action<RazorProjectEngineBuilder>? innerConfigure)
        {
            return RazorProjectEngine.Create(configuration, fileSystem, b =>
            {
                innerConfigure?.Invoke(b);
                outerConfigure?.Invoke(b);
            });
        }
    }
}
