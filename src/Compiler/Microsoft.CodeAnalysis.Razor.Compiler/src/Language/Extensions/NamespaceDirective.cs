// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public static class NamespaceDirective
{
    public static readonly DirectiveDescriptor Descriptor = DirectiveDescriptor.CreateSingleLine(
        "namespace",
        builder =>
        {
            builder.AddNamespaceToken(
                Resources.NamespaceDirective_NamespaceToken_Name,
                Resources.NamespaceDirective_NamespaceToken_Description);
            builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
            builder.Description = Resources.NamespaceDirective_Description;
        });

    public static RazorProjectEngineBuilder Register(RazorProjectEngineBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.AddDirective(Descriptor, FileKinds.Legacy, FileKinds.Component, FileKinds.ComponentImport);
        return builder;
    }
}
