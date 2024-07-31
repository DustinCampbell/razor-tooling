// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal static class AttributeDirective
{
    public static readonly DirectiveDescriptor Descriptor = DirectiveDescriptor.CreateSingleLine(
        "attribute",
        builder =>
        {
            builder.AddAttributeToken(ComponentResources.AttributeDirective_AttributeToken_Name, ComponentResources.AttributeDirective_AttributeToken_Description);
            builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
            builder.Description = ComponentResources.AttributeDirective_Description;
        });

    public static void Register(RazorProjectEngineBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.AddDirective(Descriptor, FileKinds.Legacy, FileKinds.Component, FileKinds.ComponentImport);
        builder.Features.Add(new AttributeDirectivePass());
    }
}
