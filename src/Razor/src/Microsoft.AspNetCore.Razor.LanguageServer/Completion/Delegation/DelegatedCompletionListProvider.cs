// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Hosting;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion.Delegation;

internal class DelegatedCompletionListProvider(
    IEnumerable<DelegatedCompletionResponseRewriter> responseRewriters,
    IDocumentMappingService documentMappingService,
    IClientConnection clientConnection,
    CompletionListCache completionListCache)
{
    private static readonly FrozenSet<string> s_razorTriggerCharacters = FrozenSet.ToFrozenSet(["@"]);
    private static readonly FrozenSet<string> s_csharpTriggerCharacters = FrozenSet.ToFrozenSet([" ", "(", "=", "#", ".", "<", "[", "{", "\"", "/", ":", "~"]);
    private static readonly FrozenSet<string> s_htmlTriggerCharacters = FrozenSet.ToFrozenSet([":", "@", "#", ".", "!", "*", ",", "(", "[", "-", "<", "&", "\\", "/", "'", "\"", "=", ":", " ", "`"]);
    private static readonly FrozenSet<string> s_allTriggerCharacters = FrozenSet.ToFrozenSet([
        .. s_csharpTriggerCharacters.Items,
        .. s_htmlTriggerCharacters.Items,
        .. s_razorTriggerCharacters.Items
    ]);

    private readonly ImmutableArray<DelegatedCompletionResponseRewriter> _responseRewriters = responseRewriters.OrderByAsArray(static rewriter => rewriter.Order);
    private readonly IDocumentMappingService _documentMappingService = documentMappingService;
    private readonly IClientConnection _clientConnection = clientConnection;
    private readonly CompletionListCache _completionListCache = completionListCache;

    // virtual for tests
    public virtual FrozenSet<string> TriggerCharacters => s_allTriggerCharacters;

    // virtual for tests
    public virtual async Task<VSInternalCompletionList?> GetCompletionListAsync(
        int absoluteIndex,
        VSInternalCompletionContext completionContext,
        DocumentContext documentContext,
        VSInternalClientCapabilities clientCapabilities,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var codeDocument = await documentContext.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        var positionInfo = _documentMappingService.GetPositionInfo(codeDocument, absoluteIndex);

        if (positionInfo.LanguageKind == RazorLanguageKind.Razor)
        {
            // Nothing to delegate to.
            return null;
        }

        TextEdit? provisionalTextEdit = null;
        if (TryGetProvisionalCompletionInfo(codeDocument, completionContext, positionInfo, out var provisionalCompletion))
        {
            provisionalTextEdit = provisionalCompletion.ProvisionalTextEdit;
            positionInfo = provisionalCompletion.ProvisionalPositionInfo;
        }

        completionContext = RewriteContext(completionContext, positionInfo.LanguageKind);

        var shouldIncludeSnippets = ShouldIncludeSnippets(codeDocument, absoluteIndex);

        var delegatedParams = new DelegatedCompletionParams(
            documentContext.GetTextDocumentIdentifierAndVersion(),
            positionInfo.Position,
            positionInfo.LanguageKind,
            completionContext,
            provisionalTextEdit,
            shouldIncludeSnippets,
            correlationId);

        var delegatedResponse = await _clientConnection.SendRequestAsync<DelegatedCompletionParams, VSInternalCompletionList?>(
            LanguageServerConstants.RazorCompletionEndpointName,
            delegatedParams,
            cancellationToken).ConfigureAwait(false);

        if (delegatedResponse is null)
        {
            // If we don't get a response from the delegated server, we have to make sure to return an incomplete completion
            // list. When a user is typing quickly, the delegated request from the first keystroke will fail to synchronize,
            // so if we return a "complete" list then the query won't re-query us for completion once the typing stops/slows
            // so we'd only ever return Razor completion items.
            return new VSInternalCompletionList() { IsIncomplete = true, Items = [] };
        }

        var rewrittenResponse = delegatedResponse;

        foreach (var rewriter in _responseRewriters)
        {
            rewrittenResponse = await rewriter.RewriteAsync(
                rewrittenResponse,
                absoluteIndex,
                documentContext,
                delegatedParams,
                cancellationToken).ConfigureAwait(false);
        }

        var completionCapability = clientCapabilities?.TextDocument?.Completion as VSInternalCompletionSetting;
        var resolutionContext = new DelegatedCompletionResolutionContext(delegatedParams, rewrittenResponse.Data);
        var resultId = _completionListCache.Add(rewrittenResponse, resolutionContext);
        rewrittenResponse.SetResultId(resultId, completionCapability);

        return rewrittenResponse;
    }

    private static bool ShouldIncludeSnippets(RazorCodeDocument codeDocument, int absoluteIndex)
    {
        var tree = codeDocument.GetSyntaxTree();

        var token = tree.Root.FindToken(absoluteIndex, includeWhitespace: false);
        var node = token.Parent;
        var startOrEndTag = node?.FirstAncestorOrSelf<SyntaxNode>(n => RazorSyntaxFacts.IsAnyStartTag(n) || RazorSyntaxFacts.IsAnyEndTag(n));

        if (startOrEndTag is null)
        {
            return token.Kind is not (SyntaxKind.OpenAngle or SyntaxKind.CloseAngle);
        }

        if (startOrEndTag.Span.Start == absoluteIndex)
        {
            // We're at the start of the tag, we should include snippets. This is the case for things like $$<div></div> or <div>$$</div>, since the
            // index is right associative to the token when using FindToken.
            return true;
        }

        return !startOrEndTag.Span.Contains(absoluteIndex);
    }

    private static VSInternalCompletionContext RewriteContext(VSInternalCompletionContext context, RazorLanguageKind languageKind)
    {
        if (context.TriggerKind != CompletionTriggerKind.TriggerCharacter ||
            context.TriggerCharacter is not { } triggerCharacter)
        {
            // Non-triggered based completion, the existing context is valid.
            return context;
        }

        if (languageKind == RazorLanguageKind.CSharp && s_csharpTriggerCharacters.Contains(triggerCharacter))
        {
            // C# trigger character for C# content
            return context;
        }

        if (languageKind == RazorLanguageKind.Html && s_htmlTriggerCharacters.Contains(triggerCharacter))
        {
            // HTML trigger character for HTML content
            return context;
        }

        // Trigger character not associated with the current language. Transform the context into an invoked context.
        var rewrittenContext = new VSInternalCompletionContext()
        {
            InvokeKind = context.InvokeKind,
            TriggerKind = CompletionTriggerKind.Invoked,
        };

        if (languageKind == RazorLanguageKind.CSharp && s_razorTriggerCharacters.Contains(triggerCharacter))
        {
            // The C# language server will not return any completions for the '@' character unless we
            // send the completion request explicitly.
            rewrittenContext.InvokeKind = VSInternalCompletionInvokeKind.Explicit;
        }

        return rewrittenContext;
    }

    private bool TryGetProvisionalCompletionInfo(
        RazorCodeDocument codeDocument,
        VSInternalCompletionContext completionContext,
        DocumentPositionInfo positionInfo,
        [NotNullWhen(true)] out ProvisionalCompletionInfo? provisionalCompletionInfo)
    {
        if (positionInfo.LanguageKind != RazorLanguageKind.Html ||
            completionContext.TriggerKind != CompletionTriggerKind.TriggerCharacter ||
            completionContext.TriggerCharacter != ".")
        {
            // Invalid provisional completion context
            provisionalCompletionInfo = null;
            return false;
        }

        if (positionInfo.Position.Character == 0)
        {
            // We're at the start of line. Can't have provisional completions here.
            provisionalCompletionInfo = null;
            return false;
        }

        var previousCharacterPositionInfo = _documentMappingService.GetPositionInfo(codeDocument, positionInfo.HostDocumentIndex - 1);

        if (previousCharacterPositionInfo.LanguageKind != RazorLanguageKind.CSharp)
        {
            provisionalCompletionInfo = null;
            return false;
        }

        var previousPosition = previousCharacterPositionInfo.Position;

        // Edit the CSharp projected document to contain a '.'. This allows C# completion to provide valid
        // completion items for moments when a user has typed a '.' that's typically interpreted as Html.
        var addProvisionalDot = VsLspFactory.CreateTextEdit(previousPosition, ".");

        var provisionalPositionInfo = new DocumentPositionInfo(
            RazorLanguageKind.CSharp,
            VsLspFactory.CreatePosition(
                previousPosition.Line,
                previousPosition.Character + 1),
            previousCharacterPositionInfo.HostDocumentIndex + 1);

        provisionalCompletionInfo = new ProvisionalCompletionInfo(addProvisionalDot, provisionalPositionInfo);
        return true;
    }

    private record class ProvisionalCompletionInfo(TextEdit ProvisionalTextEdit, DocumentPositionInfo ProvisionalPositionInfo);
}
