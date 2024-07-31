// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal static class ComponentLayoutDirective
{
    public static readonly DirectiveDescriptor Descriptor = DirectiveDescriptor.CreateSingleLine(
        "layout",
        builder =>
        {
            builder.AddTypeToken(ComponentResources.LayoutDirective_TypeToken_Name, ComponentResources.LayoutDirective_TypeToken_Description);
            builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
            builder.Description = ComponentResources.LayoutDirective_Description;
        });

    public static void Register(RazorProjectEngineBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.AddDirective(Descriptor, FileKinds.Component, FileKinds.ComponentImport);
        builder.Features.Add(new ComponentLayoutDirectivePass());
    }
}
