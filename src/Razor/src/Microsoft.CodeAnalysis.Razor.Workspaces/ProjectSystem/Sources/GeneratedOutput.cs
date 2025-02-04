// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem.Sources;

internal sealed record class GeneratedOutput(RazorCodeDocument CodeDocument, VersionStamp ImportsVersion);
