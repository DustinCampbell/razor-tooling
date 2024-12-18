// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using RazorProject = Microsoft.CodeAnalysis.Razor.ProjectSystem.RazorProject;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal class TestTagHelperResolver(ImmutableArray<TagHelperDescriptor> tagHelpers) : ITagHelperResolver
{
    public ImmutableArray<TagHelperDescriptor> TagHelpers { get; } = tagHelpers;

    public ValueTask<ImmutableArray<TagHelperDescriptor>> GetTagHelpersAsync(
        Project roslynProject,
        RazorProject project,
        CancellationToken cancellationToken)
    {
        return new(TagHelpers);
    }
}
