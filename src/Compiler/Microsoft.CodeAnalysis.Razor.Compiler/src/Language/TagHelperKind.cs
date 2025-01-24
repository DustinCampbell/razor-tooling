// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

public enum TagHelperKind
{
    Default,

    Component,
    Bind,
    ChildContent,
    EventHandler,
    FormName,
    Key,
    Ref,
    RenderMode,
    Splat,

    ViewComponent,

    FirstComponent = Component,
    LastComponent = Splat
}
