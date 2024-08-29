// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.ExternalAccess.LegacyEditor;

internal static partial class RazorWrapperFactory
{
    /// <summary>
    ///  This isn't exactly a "wrapper", since it doesn't wrap an existing object. Instead, it provides
    ///  an implementation of <see cref="IRazorTagHelperFactsService"/> that delegates to various extension methods.
    /// </summary>
    private sealed class TagHelperFactsServiceWrapper : IRazorTagHelperFactsService
    {
        public static readonly TagHelperFactsServiceWrapper Instance = new();

        private TagHelperFactsServiceWrapper()
        {
        }

        public ImmutableArray<IRazorBoundAttributeDescriptor> GetBoundTagHelperAttributes(
            IRazorTagHelperDocumentContext documentContext,
            string attributeName,
            IRazorTagHelperBinding binding)
        {
            var result = Unwrap(binding).GetBoundTagHelperAttributes(attributeName);

            return WrapAll(result, Wrap);
        }

        public IRazorTagHelperBinding? GetTagHelperBinding(
            IRazorTagHelperDocumentContext documentContext,
            string? tagName,
            IEnumerable<KeyValuePair<string, string>> attributes,
            string? parentTag,
            bool parentIsTagHelper)
            => Unwrap(documentContext).TryGetTagHelperBinding(tagName, attributes.ToImmutableArray(), parentTag, parentIsTagHelper, out var binding)
                ? WrapTagHelperBinding(binding)
                : null;

        public ImmutableArray<IRazorTagHelperDescriptor> GetTagHelpersGivenTag(IRazorTagHelperDocumentContext documentContext, string tagName, string? parentTag)
        {
            var result = Unwrap(documentContext).GetTagHelpersGivenTag(tagName, parentTag);

            return WrapAll(result, Wrap);
        }
    }
}
