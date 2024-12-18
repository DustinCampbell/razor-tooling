// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting;

internal class TestHtmlFormatter : IHtmlFormatter
{
    public Task<ImmutableArray<TextChange>> GetDocumentFormattingEditsAsync(IRazorDocument document, Uri uri, FormattingOptions options, CancellationToken cancellationToken)
    {
        return SpecializedTasks.EmptyImmutableArray<TextChange>();
    }

    public Task<ImmutableArray<TextChange>> GetOnTypeFormattingEditsAsync(IRazorDocument document, Uri uri, Position position, string triggerCharacter, FormattingOptions options, CancellationToken cancellationToken)
    {
        return SpecializedTasks.EmptyImmutableArray<TextChange>();
    }
}
