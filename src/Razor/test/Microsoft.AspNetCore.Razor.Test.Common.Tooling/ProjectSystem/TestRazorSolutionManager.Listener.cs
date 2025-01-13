// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.Test.Common.ProjectSystem;

internal partial class TestRazorSolutionManager
{
    public partial class Listener : IEnumerable<ProjectChangeEventArgs>, IDisposable
    {
        private readonly TestRazorSolutionManager _solutionManager;
        private readonly List<ProjectChangeEventArgs> _notifications;

        public Listener(TestRazorSolutionManager solutionManager)
        {
            _solutionManager = solutionManager;
            _solutionManager.Changed += SolutionManager_Changed;

            _notifications = [];
        }

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        {
            _solutionManager.Changed -= SolutionManager_Changed;
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public IEnumerator<ProjectChangeEventArgs> GetEnumerator()
        {
            foreach (var notification in _notifications)
            {
                yield return notification;
            }
        }

        private void SolutionManager_Changed(object? sender, ProjectChangeEventArgs e)
        {
            _notifications.Add(e);
        }
    }
}
