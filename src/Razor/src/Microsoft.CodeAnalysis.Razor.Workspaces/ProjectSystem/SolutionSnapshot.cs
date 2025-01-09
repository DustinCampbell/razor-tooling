// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal sealed class SolutionSnapshot(SolutionState state) : ISolutionSnapshot
{
    private readonly SolutionState _state = state;

    private readonly object _gate = new();
    private readonly Dictionary<ProjectKey, ProjectSnapshot> _projectKeyToProjectMap = [];

    public IEnumerable<IProjectSnapshot> Projects => throw new NotImplementedException();

    public bool ContainsProject(ProjectKey projectKey)
    {
        // PERF: It's intentional that we call _projectKeyToProjectMap.ContainsKey(...)
        // before ProjectStates.ContainsKey(...), even though the latter check is
        // enough to return the correct answer. This is because _projectKeyToProjectMap is
        // a Dictionary<,>, which has O(1) lookup, and ProjectStates is an
        // ImmutableDictionary<,>, which has O(log n) lookup. So, checking _projectKeyToProjectMap
        // first is faster if the DocumentSnapshot has already been created.

        lock (_gate)
        {
            if (_projectKeyToProjectMap.ContainsKey(projectKey))
            {
                return true;
            }
        }

        return _state.ProjectStates.ContainsKey(projectKey);
    }

    public bool TryGetProject(ProjectKey projectKey, [NotNullWhen(true)] out ProjectSnapshot? project)
    {
        lock (_gate)
        {
            // Have we already seen this project? If so, return it!
            if (_projectKeyToProjectMap.TryGetValue(projectKey, out project))
            {
                return true;
            }

            // Do we have ProjectState for this document? If not, we're done!
            if (!_state.ProjectStates.TryGetValue(projectKey, out var state))
            {
                project = null;
                return false;
            }

            // If we have ProjectState, go ahead and create a new DocumentSnapshot.
            project = new ProjectSnapshot(this, state);
            _projectKeyToProjectMap.Add(projectKey, project);

            return true;
        }
    }

    bool ISolutionSnapshot.TryGetProject(ProjectKey projectKey, [NotNullWhen(true)] out IProjectSnapshot? project)
    {
        if (TryGetProject(projectKey, out var result))
        {
            project = result;
            return true;
        }

        project = null;
        return false;
    }

    public ImmutableArray<ProjectKey> GetProjectKeysWithFilePath(string filePath)
    {
        using var result = new PooledArrayBuilder<ProjectKey>(capacity: _state.ProjectStates.Count);

        foreach (var (projectKey, projectState) in _state.ProjectStates)
        {
            if (FilePathComparer.Instance.Equals(projectState.HostProject.FilePath, filePath))
            {
                result.Add(projectKey);
            }
        }

        return result.DrainToImmutable();
    }
}
