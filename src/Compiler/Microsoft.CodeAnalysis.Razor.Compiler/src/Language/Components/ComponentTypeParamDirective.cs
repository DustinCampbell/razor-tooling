// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal static class ComponentTypeParamDirective
{
    public static readonly DirectiveDescriptor Descriptor = DirectiveDescriptor.CreateSingleLine(
        "typeparam",
        builder =>
        {
            builder.AddMemberToken(ComponentResources.TypeParamDirective_Token_Name, ComponentResources.TypeParamDirective_Token_Description);
            builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
            builder.Description = ComponentResources.TypeParamDirective_Description;
        });

    public static RazorProjectEngineBuilder Register(RazorProjectEngineBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.AddDirective(Descriptor, FileKinds.Component, FileKinds.ComponentImport);
        return builder;
    }
}
