// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal abstract partial class AbstractTagHelperCompletionService
{
    private sealed class Policy<T> : IPooledObjectPolicy<CompletionBuilder<T>>
        where T : TagHelperObject<T>
    {
        public static readonly Policy<T> Instance = new();

        private Policy()
        {
        }

        public CompletionBuilder<T> Create() => new();

        public bool Return(CompletionBuilder<T> builder)
        {
            builder.Clear();
            return true;
        }
    }
}
