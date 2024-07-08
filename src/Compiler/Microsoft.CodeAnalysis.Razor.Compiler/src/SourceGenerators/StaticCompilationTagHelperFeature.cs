// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators
{
    internal sealed class StaticCompilationTagHelperFeature(Compilation compilation)
        : RazorEngineFeatureBase, ITagHelperFeature
    {
        private ImmutableArray<ITagHelperDescriptorProvider> _providers;

        public void CollectDescriptors(ISymbol? targetSymbol, TagHelperDescriptorCollection.IBuilder builder)
        {
            if (_providers.IsDefault)
            {
                return;
            }

            using var context = TagHelperDescriptorProviderContext.Create(compilation, targetSymbol, builder);

            foreach (var provider in _providers)
            {
                provider.Execute(context);
            }
        }

        TagHelperDescriptorCollection ITagHelperFeature.GetDescriptors()
        {
            using var results = TagHelperDescriptorCollection.GetBuilder();
            CollectDescriptors(targetSymbol: null, results);

            return results.ToCollection();
        }

        protected override void OnInitialized()
        {
            _providers = Engine.Features
                .OfType<ITagHelperDescriptorProvider>()
                .OrderBy(f => f.Order)
                .ToImmutableArray();
        }
    }
}
