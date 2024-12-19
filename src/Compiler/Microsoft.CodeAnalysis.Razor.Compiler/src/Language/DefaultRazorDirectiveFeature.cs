// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultRazorDirectiveFeature : RazorEngineFeatureBase, IRazorDirectiveFeature, IConfigureRazorParserOptionsFeature
{
    // To maintain backwards compatibility, adding to this list will default to legacy file kind.
    public ICollection<DirectiveDescriptor> Directives
    {
        get
        {
            if (!DirectivesByFileKind.TryGetValue(RazorFileKind.Legacy, out var result))
            {
                result = [];
                DirectivesByFileKind.Add(RazorFileKind.Legacy, result);
            }

            return result;
        }
    }

    public IDictionary<RazorFileKind, ICollection<DirectiveDescriptor>> DirectivesByFileKind { get; } = new Dictionary<RazorFileKind, ICollection<DirectiveDescriptor>>();

    public int Order => 100;

    void IConfigureRazorParserOptionsFeature.Configure(RazorParserOptionsBuilder options)
    {
        ArgHelper.ThrowIfNull(options);

        options.Directives.Clear();

        var fileKind = options.FileKind is not null
            ? options.FileKind.ToRazorFileKind()
            : RazorFileKind.Legacy;

        if (DirectivesByFileKind.TryGetValue(fileKind, out var directives))
        {
            foreach (var directive in directives)
            {
                options.Directives.Add(directive);
            }
        }
    }
}
