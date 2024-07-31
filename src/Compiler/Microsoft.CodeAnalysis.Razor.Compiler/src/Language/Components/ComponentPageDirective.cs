// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal static class ComponentPageDirective
{
    public static readonly DirectiveDescriptor Descriptor = DirectiveDescriptor.CreateSingleLine(
        "page",
        builder =>
        {
            builder.AddStringToken(ComponentResources.PageDirective_RouteToken_Name, ComponentResources.PageDirective_RouteToken_Description);
            builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
            builder.Description = ComponentResources.PageDirective_Description;
        });

    public static RazorProjectEngineBuilder Register(RazorProjectEngineBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.AddDirective(Descriptor, FileKinds.Component, FileKinds.ComponentImport);
        builder.Features.Add(new ComponentPageDirectivePass());
        return builder;
    }
}
