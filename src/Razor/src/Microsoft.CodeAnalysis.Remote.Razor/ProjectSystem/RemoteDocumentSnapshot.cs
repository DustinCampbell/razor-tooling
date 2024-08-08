// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

internal class RemoteDocumentSnapshot(TextDocument textDocument, RemoteProjectSnapshot projectSnapshot) : IDocumentSnapshot
{
    private readonly TextDocument _textDocument = textDocument;
    private readonly RemoteProjectSnapshot _projectSnapshot = projectSnapshot;

    // TODO: Delete this field when the source generator is hooked up
    private Document? _generatedDocument;

    private RazorCodeDocument? _codeDocument;

    public TextDocument TextDocument => _textDocument;

    public string? FileKind => FileKinds.GetFileKindFromFilePath(FilePath);

    public string? FilePath => _textDocument.FilePath;

    public string? TargetPath => _textDocument.FilePath;

    public IProjectSnapshot Project => _projectSnapshot;

    public bool SupportsOutput => true;

    public async ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
    {
        return await _textDocument.GetTextAsync(cancellationToken);
    }

    public async ValueTask<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken)
    {
        return await _textDocument.GetTextVersionAsync(cancellationToken);
    }

    public ValueTask<RazorCodeDocument> GetGeneratedOutputAsync(CancellationToken cancellationToken)
    {
        return _codeDocument is RazorCodeDocument codeDocument
            ? new(codeDocument)
            : GetGeneratedOutputCoreAsync(cancellationToken);

        async ValueTask<RazorCodeDocument> GetGeneratedOutputCoreAsync(CancellationToken cancellationToken)
        {
            // The non-cohosted DocumentSnapshot implementation uses DocumentState to get the generated output, and we could do that too
            // but most of that code is optimized around caching pre-computed results when things change that don't affect the compilation.
            // We can't do that here because we are using Roslyn's project snapshots, which don't contain the info that Razor needs. We could
            // in future provide a side-car mechanism so we can cache things, but still take advantage of snapshots etc. but the working
            // assumption for this code is that the source generator will be used, and it will do all of that, so this implementation is naive
            // and simply compiles when asked, and if a new document snapshot comes in, we compile again. This is presumably worse for perf
            // but since we don't expect users to ever use cohosting without source generators, it's fine for now.

            var projectEngine = _projectSnapshot.GetProjectEngine_CohostOnly();
            var tagHelpers = await _projectSnapshot.GetTagHelpersAsync(cancellationToken).ConfigureAwait(false);
            var imports = await DocumentState.GetImportsAsync(this, projectEngine).ConfigureAwait(false);

            // TODO: Get the configuration for forceRuntimeCodeGeneration
            // var forceRuntimeCodeGeneration = _projectSnapshot.Configuration.LanguageServerFlags?.ForceRuntimeCodeGeneration ?? false;

            codeDocument = await DocumentState
                .GenerateCodeDocumentAsync(this, projectEngine, imports, tagHelpers, forceRuntimeCodeGeneration: false)
                .ConfigureAwait(false);

            return InterlockedOperations.Initialize(ref _codeDocument, codeDocument);
        }
    }

    public Task<SourceText> GetTextAsync() => _textDocument.GetTextAsync();

    public Task<VersionStamp> GetTextVersionAsync() => _textDocument.GetTextVersionAsync();

    public bool TryGetText([NotNullWhen(true)] out SourceText? result) => _textDocument.TryGetText(out result);

    public bool TryGetTextVersion(out VersionStamp result) => _textDocument.TryGetTextVersion(out result);

    public async Task<RazorCodeDocument> GetGeneratedOutputAsync()
    {
        return await GetGeneratedOutputAsync(CancellationToken.None);
    }

    public IDocumentSnapshot WithText(SourceText text)
    {
        var id = _textDocument.Id;
        var newDocument = _textDocument.Project.Solution.WithAdditionalDocumentText(id, text).GetAdditionalDocument(id).AssumeNotNull();

        return new RemoteDocumentSnapshot(newDocument, _projectSnapshot);
    }

    public bool TryGetGeneratedOutput([NotNullWhen(true)] out RazorCodeDocument? codeDocument)
    {
        codeDocument = _codeDocument;
        return codeDocument is not null;
    }

    public async Task<Document> GetOrAddGeneratedDocumentAsync<TArg>(TArg arg, Func<TArg, Task<Document>> createGeneratedDocument)
    {
        if (_generatedDocument is Document generatedDocument)
        {
            return generatedDocument;
        }

        generatedDocument = await createGeneratedDocument(arg);
        return InterlockedOperations.Initialize(ref _generatedDocument, generatedDocument);
    }
}
