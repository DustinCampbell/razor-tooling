// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class ConfigureDirectivesFeature : RazorEngineFeatureBase, IConfigureParserOptionsFeature
{
    public int Order => 100;

    private readonly Dictionary<string, ImmutableArray<DirectiveDescriptor>.Builder> _fileKindToDirectivesMap = new(StringComparer.OrdinalIgnoreCase);

    public void AddDirective(DirectiveDescriptor directive)
    {
        AddDirective(directive, FileKinds.Legacy);
    }

    public void AddDirective(DirectiveDescriptor directive, params ReadOnlySpan<string> fileKinds)
    {
        lock (_fileKindToDirectivesMap)
        {
            foreach (var fileKind in fileKinds)
            {
                var directives = _fileKindToDirectivesMap.GetOrAdd(fileKind, static _ => ImmutableArray.CreateBuilder<DirectiveDescriptor>());

                directives.Add(directive);
            }
        }
    }

    public ImmutableArray<DirectiveDescriptor> GetDirectives(string? fileKind = null)
    {
        fileKind ??= FileKinds.Legacy;

        lock (_fileKindToDirectivesMap)
        {
            return _fileKindToDirectivesMap.TryGetValue(fileKind, out var directives)
                ? directives.ToImmutable()
                : [];
        }
    }

    void IConfigureParserOptionsFeature.Configure(RazorParserOptions.Builder builder)
    {
        builder.Directives = GetDirectives(builder.FileKind);
    }
}
