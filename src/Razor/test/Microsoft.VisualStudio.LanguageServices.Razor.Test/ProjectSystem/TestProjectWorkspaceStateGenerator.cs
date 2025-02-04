// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.Razor.ProjectSystem;

internal class TestProjectWorkspaceStateGenerator : IProjectWorkspaceStateGenerator
{
    private readonly List<TestUpdate> _updates = [];

    public IReadOnlyList<TestUpdate> Updates => _updates;

    public void EnqueueUpdate(ProjectId? projectId, ProjectKey projectKey)
    {
        var update = new TestUpdate(projectId, projectKey);
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

    public record TestUpdate(ProjectId? ProjectId, ProjectKey ProjectKey)
    {
        public bool IsCancelled { get; set; }

        public override string ToString()
        {
            using var _ = StringBuilderPool.GetPooledObject(out var builder);

            builder.Append($"{{{nameof(ProjectId)} = ");

            if (ProjectId is null)
            {
                builder.Append("<null>");
            }
            else
            {
                builder.Append(ProjectId);
            }

            builder.Append($", {nameof(ProjectKey)} = {ProjectKey}}}");

            return builder.ToString();
        }
    }
}
