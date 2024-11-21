// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using static Microsoft.AspNetCore.Razor.Language.CommonMetadata;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal static class SimpleTagHelpers
{
    public static ImmutableArray<TagHelperDescriptor> Default { get; }

    static SimpleTagHelpers()
    {
        var builder1 = TagHelperDescriptorBuilder.Create("Test1TagHelper", "TestAssembly");
        builder1.TypeName = "Test1TagHelper";
        builder1.TagMatchingRule(rule => rule.TagName = "test1");
        builder1.BindAttribute(attribute =>
        {
            attribute.Name = "bool-val";
            attribute.PropertyName = "BoolVal";
            attribute.TypeName = typeof(bool).FullName;
        });
        builder1.BindAttribute(attribute =>
        {
            attribute.Name = "int-val";
            attribute.PropertyName = "IntVal";
            attribute.TypeName = typeof(int).FullName;
        });

        var builder1WithRequiredParent = TagHelperDescriptorBuilder.Create("Test1TagHelper.SomeChild", "TestAssembly");
        builder1WithRequiredParent.TypeName = "Test1TagHelper.SomeChild";
        builder1WithRequiredParent.TagMatchingRule(rule =>
        {
            rule.TagName = "SomeChild";
            rule.ParentTag = "test1";
        });
        builder1WithRequiredParent.BindAttribute(attribute =>
        {
            attribute.Name = "attribute";
            attribute.PropertyName = "Attribute";
            attribute.TypeName = typeof(string).FullName;
        });

        var builder2 = TagHelperDescriptorBuilder.Create("Test2TagHelper", "TestAssembly");
        builder2.TypeName = "Test2TagHelper";
        builder2.TagMatchingRule(rule => rule.TagName = "test2");
        builder2.BindAttribute(attribute =>
        {
            attribute.Name = "bool-val";
            attribute.PropertyName = "BoolVal";
            attribute.TypeName = typeof(bool).FullName;
        });
        builder2.BindAttribute(attribute =>
        {
            attribute.Name = "int-val";
            attribute.PropertyName = "IntVal";
            attribute.TypeName = typeof(int).FullName;
        });

        var builder3 = TagHelperDescriptorBuilder.Create(TagHelperKind.Component, "Component1TagHelper", "TestAssembly");
        builder3.TypeName = "Component1";
        builder3.TypeNamespace = "System";
        builder3.TypeNameIdentifier = "Component1";
        builder3.IsComponentFullyQualifiedNameMatch = true;
        builder3.TagMatchingRule(rule => rule.TagName = "Component1");
        builder3.BindAttribute(attribute =>
        {
            attribute.Name = "bool-val";
            attribute.PropertyName = "BoolVal";
            attribute.TypeName = typeof(bool).FullName;
        });
        builder3.BindAttribute(attribute =>
        {
            attribute.Name = "int-val";
            attribute.PropertyName = "IntVal";
            attribute.TypeName = typeof(int).FullName;
        });
        builder3.BindAttribute(attribute =>
        {
            attribute.Name = "Title";
            attribute.PropertyName = "Title";
            attribute.TypeName = typeof(string).FullName;
        });

        var textComponent = TagHelperDescriptorBuilder.Create(TagHelperKind.Component, "TextTagHelper", "TestAssembly");
        textComponent.IsComponentFullyQualifiedNameMatch = true;
        textComponent.TypeName = "Text";
        textComponent.TypeNamespace = "System";
        textComponent.TypeNameIdentifier = "Text";
        textComponent.TagMatchingRule(rule => rule.TagName = "Text");

        var directiveAttribute1 = TagHelperDescriptorBuilder.Create(TagHelperKind.Component, "TestDirectiveAttribute", "TestAssembly");
        directiveAttribute1.TypeName = "TestDirectiveAttribute";
        directiveAttribute1.IsComponentFullyQualifiedNameMatch = true;
        directiveAttribute1.ClassifyAttributesOnly = true;
        directiveAttribute1.TagMatchingRule(rule =>
        {
            rule.TagName = "*";
            rule.RequireAttributeDescriptor(b =>
            {
                b.Name = "@test";
                b.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch;
            });
        });
        directiveAttribute1.TagMatchingRule(rule =>
        {
            rule.TagName = "*";
            rule.RequireAttributeDescriptor(b =>
            {
                b.Name = "@test";
                b.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.FullMatch;
            });
        });
        directiveAttribute1.BindAttribute(attribute =>
        {
            attribute.Name = "@test";
            attribute.IsDirectiveAttribute = true;
            attribute.PropertyName = "Test";
            attribute.TypeName = typeof(string).FullName;

            attribute.BindAttributeParameter(parameter =>
            {
                parameter.Name = "something";
                parameter.TypeName = typeof(string).FullName;

                parameter.PropertyName = "Something";
            });
        });

        var directiveAttribute2 = TagHelperDescriptorBuilder.Create(TagHelperKind.Component, "MinimizedDirectiveAttribute", "TestAssembly");
        directiveAttribute2.TypeName = "TestDirectiveAttribute";
        directiveAttribute2.IsComponentFullyQualifiedNameMatch = true;
        directiveAttribute2.ClassifyAttributesOnly = true;
        directiveAttribute2.TagMatchingRule(rule =>
        {
            rule.TagName = "*";
            rule.RequireAttributeDescriptor(b =>
            {
                b.Name = "@minimized";
                b.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch;
            });
        });
        directiveAttribute2.TagMatchingRule(rule =>
        {
            rule.TagName = "*";
            rule.RequireAttributeDescriptor(b =>
            {
                b.Name = "@minimized";
                b.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.FullMatch;
            });
        });
        directiveAttribute2.BindAttribute(attribute =>
        {
            attribute.Name = "@minimized";
            attribute.IsDirectiveAttribute = true;
            attribute.PropertyName = "Minimized";
            attribute.TypeName = typeof(bool).FullName;

            attribute.BindAttributeParameter(parameter =>
            {
                parameter.Name = "something";
                parameter.TypeName = typeof(string).FullName;

                parameter.PropertyName = "Something";
            });
        });

        var directiveAttribute3 = TagHelperDescriptorBuilder.Create(TagHelperKind.EventHandler, RuntimeKind.None, "OnClickDirectiveAttribute", "TestAssembly");
        directiveAttribute3.TypeName = "OnClickDirectiveAttribute";
        directiveAttribute3.TypeNamespace = "Microsoft.AspNetCore.Components.Web";
        directiveAttribute3.TypeNameIdentifier = "EventHandlers";
        directiveAttribute3.IsComponentFullyQualifiedNameMatch = true;
        directiveAttribute3.ClassifyAttributesOnly = true;
        directiveAttribute3.TagMatchingRule(rule =>
        {
            rule.TagName = "*";
            rule.RequireAttributeDescriptor(b =>
            {
                b.Name = "@onclick";
                b.IsDirectiveAttribute = true;
                b.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.FullMatch;
            });
        });
        directiveAttribute3.TagMatchingRule(rule =>
        {
            rule.TagName = "*";
            rule.RequireAttributeDescriptor(b =>
            {
                b.Name = "@onclick";
                b.IsDirectiveAttribute = true;
                b.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch;
            });
        });
        directiveAttribute3.BindAttribute(attribute =>
        {
            attribute.Name = "@onclick";
            attribute.IsDirectiveAttribute = true;
            attribute.PropertyName = "onclick";
            attribute.SetMetadata(IsWeaklyTyped);
            attribute.TypeName = "Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>";
        });
        directiveAttribute3.SetMetadata(
            new KeyValuePair<string, string>(ComponentMetadata.EventHandler.EventArgsType, "Microsoft.AspNetCore.Components.Web.MouseEventArgs"));

        var htmlTagMutator = TagHelperDescriptorBuilder.Create("HtmlMutator", "TestAssembly");
        htmlTagMutator.TypeName = "HtmlMutator";
        htmlTagMutator.TagMatchingRule(rule =>
        {
            rule.TagName = "title";
            rule.RequireAttributeDescriptor(attributeRule =>
            {
                attributeRule.Name = "mutator";
            });
        });
        htmlTagMutator.BindAttribute(attribute =>
        {
            attribute.Name = "Extra";
            attribute.PropertyName = "Extra";
            attribute.TypeName = typeof(bool).FullName;
        });

        Default =
        [
            builder1.Build(),
            builder1WithRequiredParent.Build(),
            builder2.Build(),
            builder3.Build(),
            textComponent.Build(),
            directiveAttribute1.Build(),
            directiveAttribute2.Build(),
            directiveAttribute3.Build(),
            htmlTagMutator.Build(),
        ];
    }
}
