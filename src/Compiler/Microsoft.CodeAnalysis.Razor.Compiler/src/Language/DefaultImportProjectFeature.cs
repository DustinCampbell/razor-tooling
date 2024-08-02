﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class DefaultImportProjectFeature : RazorProjectEngineFeatureBase, IImportProjectFeature
{
    public IReadOnlyList<RazorProjectItem> GetImports(RazorProjectItem projectItem) => [];
}
