// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public static class InheritsDirective
{
    public static readonly DirectiveDescriptor Descriptor = DirectiveDescriptor.CreateSingleLine(
        SyntaxConstants.CSharp.InheritsKeyword,
        builder =>
        {
            builder.AddTypeToken(Resources.InheritsDirective_TypeToken_Name, Resources.InheritsDirective_TypeToken_Description);
            builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
            builder.Description = Resources.InheritsDirective_Description;
        });

    public static void Register(RazorProjectEngineBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.AddDirective(Descriptor, FileKinds.Legacy, FileKinds.Component, FileKinds.ComponentImport);
        builder.Features.Add(new InheritsDirectivePass());
    }
}
