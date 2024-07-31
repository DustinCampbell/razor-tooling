// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public static class FunctionsDirective
{
    public static readonly DirectiveDescriptor Descriptor = DirectiveDescriptor.CreateCodeBlock(
        SyntaxConstants.CSharp.FunctionsKeyword,
        builder =>
        {
            builder.Description = Resources.FunctionsDirective_Description;
        });

    public static void Register(RazorProjectEngineBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.AddDirective(Descriptor, FileKinds.Legacy, FileKinds.Component);
        builder.Features.Add(new FunctionsDirectivePass());
    }
}
