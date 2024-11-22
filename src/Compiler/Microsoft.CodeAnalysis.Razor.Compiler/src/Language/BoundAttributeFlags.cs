// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

[Flags]
internal enum BoundAttributeFlags
{
    CaseSensitive = 1 << 0,
    HasIndexer = 1 << 1,
    IsIndexerStringProperty = 1 << 2,
    IsIndexerBooleanProperty = 1 << 3,
    IsEnum = 1 << 4,
    IsStringProperty = 1 << 5,
    IsBooleanProperty = 1 << 6,
    IsEditorRequired = 1 << 7,
    IsDirectiveAttribute = 1 << 8,
    IsWeaklyTyped = 1 << 9,
    IsChildContentProperty = 1 << 10,
    IsEventCallbackProperty = 1 << 11,
    IsChildContentParameterNameProperty = 1 << 12
}
