// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language;

public static class RazorFileKinds
{
    private static readonly FrozenDictionary<string, RazorFileKind> s_fileKindMap = new[]
    {
        KeyValuePair.Create(FileKinds.Component, RazorFileKind.Component),
        KeyValuePair.Create(FileKinds.ComponentImport, RazorFileKind.ComponentImport),
        KeyValuePair.Create(FileKinds.Legacy, RazorFileKind.Legacy)
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static bool IsComponent(this RazorFileKind fileKind)
        => fileKind is RazorFileKind.Component or RazorFileKind.ComponentImport;

    public static bool IsComponentImport(this RazorFileKind fileKind)
        => fileKind is RazorFileKind.ComponentImport;

    public static bool IsLegacy(this RazorFileKind fileKind)
        => fileKind is RazorFileKind.Legacy;

    public static string ToLegacyFileKind(this RazorFileKind fileKind)
        => fileKind switch
        {
            RazorFileKind.Component => FileKinds.Component,
            RazorFileKind.ComponentImport => FileKinds.ComponentImport,
            RazorFileKind.Legacy => FileKinds.Legacy,
            _ => Assumed.Unreachable<string>()
        };

    public static RazorFileKind GetRazorFileKind(string fileKind)
        => s_fileKindMap.TryGetValue(fileKind, out var result)
            ? result
            : ThrowHelper.ThrowInvalidOperationException<RazorFileKind>($"Unexpected file kind value: '{fileKind}'");

    public static RazorFileKind GetComponentFileKindFromFilePath(string filePath)
    {
        ArgHelper.ThrowIfNull(filePath);

        return IsComponentImportsFileName(filePath)
            ? RazorFileKind.ComponentImport
            : RazorFileKind.Component;
    }

    public static RazorFileKind GetFileKindFromFilePath(string filePath)
    {
        ArgHelper.ThrowIfNull(filePath);

        if (IsComponentImportsFileName(filePath))
        {
            return RazorFileKind.ComponentImport;
        }

        if (IsRazorFileExtension(filePath))
        {
            return RazorFileKind.Component;
        }

        return RazorFileKind.Legacy;
    }

    private static bool IsComponentImportsFileName(string filePath)
    {
        // Note: This intentionally performs a case-sensitive match.

#if NET
        // On .NET, we can use ReadOnlySpan<char> with path.
        var fileName = Path.GetFileName(filePath.AsSpan());

        return fileName.Equals(ComponentMetadata.ImportsFileName.AsSpan(), StringComparison.Ordinal);
#else
        return string.Equals(ComponentMetadata.ImportsFileName, Path.GetFileName(filePath), StringComparison.Ordinal);
#endif
    }

    private static bool IsRazorFileExtension(string filePath)
    {
        // Note: This intentionally performs a case-insensitive match.
        const string RazorFileExtension = ".razor";

#if NET
        var fileExtension = Path.GetExtension(filePath.AsSpan());

        return fileExtension.Equals(RazorFileExtension.AsSpan(), StringComparison.OrdinalIgnoreCase);
#else
        return string.Equals(RazorFileExtension, Path.GetExtension(filePath), StringComparison.OrdinalIgnoreCase);
#endif
    }
}
