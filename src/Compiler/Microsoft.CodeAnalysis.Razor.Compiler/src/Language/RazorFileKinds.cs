// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language;

public static class RazorFileKinds
{
    private static StringComparer Comparer => StringComparer.OrdinalIgnoreCase;

    private static ReadOnlySpan<char> ComponentImportsFileName => ComponentMetadata.ImportsFileName.AsSpan();
    private static ReadOnlySpan<char> RazorExtension => ".razor".AsSpan();

    public static bool IsComponent(RazorFileKind fileKind)
        => fileKind is RazorFileKind.Component or RazorFileKind.ComponentImport;

    public static bool IsComponentImport(RazorFileKind fileKind)
        => fileKind is RazorFileKind.ComponentImport;

    public static bool IsLegacy(RazorFileKind fileKind)
        => fileKind is RazorFileKind.Legacy;

    public static RazorFileKind ToRazorFileKind(this RazorFileKind? fileKind, string? filePath = null)
        => fileKind switch
        {
            null => GetFileKindFromFilePath(filePath),
            RazorFileKind value => value
        };

    public static RazorFileKind ToRazorFileKind(this string? fileKind, string? filePath = null)
    {
        if (fileKind is null)
        {
            return GetFileKindFromFilePath(filePath);
        }

        if (Comparer.Equals(FileKinds.Component, fileKind))
        {
            return RazorFileKind.Component;
        }

        if (Comparer.Equals(FileKinds.ComponentImport, fileKind))
        {
            return RazorFileKind.ComponentImport;
        }

        if (Comparer.Equals(FileKinds.Legacy, fileKind))
        {
            return RazorFileKind.Legacy;
        }

        return Assumed.Unreachable<RazorFileKind>($"Unexpected FileKind value: {fileKind}");
    }

    public static string? ToFileKindString(this RazorFileKind fileKind)
    {
        return fileKind switch
        {
            RazorFileKind.Component => FileKinds.Component,
            RazorFileKind.ComponentImport => FileKinds.ComponentImport,
            RazorFileKind.Legacy => FileKinds.Legacy,
            RazorFileKind.None => null,

            _ => Assumed.Unreachable<string?>($"Unexpected RazorFileKind value: {fileKind}")
        };
    }

    public static RazorFileKind GetComponentFileKindFromFilePath(string filePath)
    {
        ArgHelper.ThrowIfNull(filePath);

        if (GetFileNameSpan(filePath).Equals(ComponentImportsFileName, StringComparison.Ordinal))
        {
            return RazorFileKind.ComponentImport;
        }

        return RazorFileKind.Component;
    }

    public static RazorFileKind GetFileKindFromFilePath(string? filePath)
    {
        if (filePath is null)
        {
            return RazorFileKind.None;
        }

        Debug.Assert(RazorExtension.Length < ComponentImportsFileName.Length);

        if (filePath.Length >= RazorExtension.Length)
        {
            if (GetFileNameSpan(filePath).Equals(ComponentImportsFileName, StringComparison.Ordinal))
            {
                return RazorFileKind.ComponentImport;
            }

            if (GetExtensionSpan(filePath).Equals(RazorExtension, StringComparison.OrdinalIgnoreCase))
            {
                return RazorFileKind.Component;
            }
        }

        return RazorFileKind.Legacy;
    }

    private static ReadOnlySpan<char> GetFileNameSpan(string path)
    {
        // On .NET Core, we can use the Path.GetFileName overload that slices the path span.
#if NET
        return Path.GetFileName(path.AsSpan());
#else
        return Path.GetFileName(path).AsSpan();
#endif
    }

    private static ReadOnlySpan<char> GetExtensionSpan(string path)
    {
        // On .NET Core, we can use the Path.GetExtension overload that slices the path span.
#if NET
        return Path.GetExtension(path.AsSpan());
#else
        return Path.GetExtension(path).AsSpan();
#endif
    }
}
