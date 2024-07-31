// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public static class SectionDirective
{
    public static readonly DirectiveDescriptor Descriptor = DirectiveDescriptor.CreateRazorBlock(
        SyntaxConstants.CSharp.SectionKeyword,
        builder =>
        {
            builder.AddMemberToken(Resources.SectionDirective_NameToken_Name, Resources.SectionDirective_NameToken_Description);
            builder.Description = Resources.SectionDirective_Description;
        });

    public static void Register(RazorProjectEngineBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.AddDirective(Descriptor, FileKinds.Legacy, FileKinds.Component);
        builder.Features.Add(new SectionDirectivePass());
        builder.AddTargetExtension(new SectionTargetExtension());
    }
}
