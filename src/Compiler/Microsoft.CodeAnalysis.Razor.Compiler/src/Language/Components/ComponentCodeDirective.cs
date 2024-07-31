// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Components;

public static class ComponentCodeDirective
{
    public static readonly DirectiveDescriptor Descriptor = DirectiveDescriptor.CreateCodeBlock(
        "code",
        builder =>
        {
            builder.Description = Resources.FunctionsDirective_Description;
        });

    public static void Register(RazorProjectEngineBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.AddDirective(Descriptor, FileKinds.Component);
    }
}
