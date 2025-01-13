// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.VisualStudio.Razor.ProjectSystem;

[Export(typeof(RazorSolutionManager))]
[method: ImportingConstructor]
internal sealed class VisualStudioRazorSolutionManager(
    IProjectEngineFactoryProvider projectEngineFactoryProvider,
    LanguageServerFeatureOptions languageServerFeatureOptions,
    ILoggerFactory loggerFactory)
    : RazorSolutionManager(projectEngineFactoryProvider, languageServerFeatureOptions.ToCompilerOptions(), loggerFactory)
{
}
