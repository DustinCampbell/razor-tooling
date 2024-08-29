// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

internal static class TagHelperBindingExtensions
{
    public static ImmutableArray<BoundAttributeDescriptor> GetBoundTagHelperAttributes(this TagHelperBinding binding, string attributeName)
    {
        using var matchingBoundAttributes = new PooledArrayBuilder<BoundAttributeDescriptor>();

        foreach (var tagHelper in binding.Descriptors)
        {
            foreach (var boundAttribute in tagHelper.BoundAttributes)
            {
                if (TagHelperMatchingConventions.CanSatisfyBoundAttribute(attributeName, boundAttribute))
                {
                    matchingBoundAttributes.Add(boundAttribute);

                    // Only one bound attribute can match an attribute
                    break;
                }
            }
        }

        return matchingBoundAttributes.DrainToImmutable();
    }
}
