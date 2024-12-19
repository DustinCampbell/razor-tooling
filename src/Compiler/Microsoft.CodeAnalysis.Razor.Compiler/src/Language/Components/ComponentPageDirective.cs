// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal class ComponentPageDirective
{
    public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
        "page",
        DirectiveKind.SingleLine,
        builder =>
        {
            builder.AddStringToken(ComponentResources.PageDirective_RouteToken_Name, ComponentResources.PageDirective_RouteToken_Description);
            builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
            builder.Description = ComponentResources.PageDirective_Description;
        });

    public static RazorProjectEngineBuilder Register(RazorProjectEngineBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.AddDirective(Directive, RazorFileKind.Component, RazorFileKind.ComponentImport);
        builder.Features.Add(new ComponentPageDirectivePass());
        return builder;
    }
}
