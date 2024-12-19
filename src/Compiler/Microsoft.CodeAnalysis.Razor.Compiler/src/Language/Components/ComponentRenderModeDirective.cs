// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentRenderModeDirective
{
    public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
       "rendermode",
       DirectiveKind.SingleLine,
       builder =>
       {
           builder.AddIdentifierOrExpression(ComponentResources.RenderModeDirective_Token_Name, ComponentResources.RenderModeDirective_Token_Description);
           builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
           builder.Description = ComponentResources.RenderModeDirective_Documentation;
       });

    public static void Register(RazorProjectEngineBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.AddDirective(Directive, RazorFileKind.Component);
        builder.Features.Add(new ComponentRenderModeDirectivePass());
    }
}
