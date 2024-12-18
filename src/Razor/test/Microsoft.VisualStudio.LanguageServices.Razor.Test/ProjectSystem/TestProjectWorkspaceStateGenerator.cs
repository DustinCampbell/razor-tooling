// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.Razor.ProjectSystem;

internal class TestProjectWorkspaceStateGenerator : IProjectWorkspaceStateGenerator
{
    private readonly List<TestUpdate> _updates = [];

    public IReadOnlyList<TestUpdate> Updates => _updates;

    public void EnqueueUpdate(Project? roslynProject, RazorProject project)
    {
        var update = new TestUpdate(roslynProject, project);
        _updates.Add(update);
    }

    public void CancelUpdates()
    {
        foreach (var update in _updates)
        {
            update.IsCancelled = true;
        }
    }

    public void Clear()
    {
        _updates.Clear();
    }

    public record TestUpdate(Project? RoslynProject, RazorProject Project)
    {
        public bool IsCancelled { get; set; }

        public override string ToString()
        {
            using var _ = StringBuilderPool.GetPooledObject(out var builder);

            builder.Append($"{{{nameof(RoslynProject)} = ");

            if (RoslynProject is null)
            {
                builder.Append("<null>");
            }
            else
            {
                builder.Append(RoslynProject.Name);
            }

            builder.Append($", {nameof(Project)} = {Project.DisplayName}}}");

            return builder.ToString();
        }
    }
}
