// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Frozen;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion;

internal static class VSInternalCompletionContextExtensions
{
    public static bool IsValidTrigger(this VSInternalCompletionContext completionContext, FrozenSet<string> allowedTriggerCharacters)
        => completionContext.TriggerKind != CompletionTriggerKind.TriggerCharacter ||
           completionContext.TriggerCharacter is null ||
           allowedTriggerCharacters.Contains(completionContext.TriggerCharacter);
}
