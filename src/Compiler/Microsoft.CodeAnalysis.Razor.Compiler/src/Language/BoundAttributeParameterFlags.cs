﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

[Flags]
internal enum BoundAttributeParameterFlags
{
    CaseSensitive = 1 << 0,
    IsEnum = 1 << 1,
    IsStringProperty = 1 << 2,
    IsBooleanProperty = 1 << 3
}
