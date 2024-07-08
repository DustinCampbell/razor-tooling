// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class TagHelperDescriptorCollection
{
    private sealed class Policy : IPooledObjectPolicy<Builder>
    {
        public static readonly Policy Instance = new();

        private Policy()
        {
        }

        public Builder Create() => [];

        public bool Return(Builder builder)
        {
            builder.Clear();

            return true;
        }
    }
}
