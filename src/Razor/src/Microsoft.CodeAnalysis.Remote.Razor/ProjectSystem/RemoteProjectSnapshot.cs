// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectEngineHost;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Compiler.CSharp;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.NET.Sdk.Razor.SourceGenerators;

namespace Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

internal sealed class RemoteProjectSnapshot : IRazorProject
{
    public RemoteSolutionSnapshot SolutionSnapshot { get; }

    public ProjectKey Key { get; }

    private readonly Project _project;
    private readonly AsyncLazy<RazorConfiguration> _lazyConfiguration;
    private readonly AsyncLazy<RazorProjectEngine> _lazyProjectEngine;
    private readonly AsyncLazy<ImmutableArray<TagHelperDescriptor>> _lazyTagHelpers;
    private readonly Dictionary<TextDocument, RemoteRazorDocument> _documentMap = [];

    public RemoteProjectSnapshot(Project project, RemoteSolutionSnapshot solutionSnapshot)
    {
        if (!project.ContainsRazorDocuments())
        {
            throw new ArgumentException(SR.Project_does_not_contain_any_Razor_documents, nameof(project));
        }

        _project = project;
        SolutionSnapshot = solutionSnapshot;
        Key = _project.ToProjectKey();

        _lazyConfiguration = AsyncLazy.Create(ComputeConfigurationAsync);
        _lazyProjectEngine = AsyncLazy.Create(ComputeProjectEngineAsync);
        _lazyTagHelpers = AsyncLazy.Create(ComputeTagHelpersAsync);
    }

    public IEnumerable<string> DocumentFilePaths
        => _project.AdditionalDocuments
            .Where(static d => d.IsRazorDocument())
            .Select(static d => d.FilePath.AssumeNotNull());

    public string FilePath => _project.FilePath.AssumeNotNull();

    public string IntermediateOutputPath => FilePathNormalizer.GetNormalizedDirectoryName(_project.CompilationOutputInfo.AssemblyPath);

    public string? RootNamespace => _project.DefaultNamespace ?? "ASP";

    public string DisplayName => _project.Name;

    public Project Project => _project;

    public LanguageVersion CSharpLanguageVersion => ((CSharpParseOptions)_project.ParseOptions.AssumeNotNull()).LanguageVersion;

    public ValueTask<ImmutableArray<TagHelperDescriptor>> GetTagHelpersAsync(CancellationToken cancellationToken)
    {
        if (_lazyTagHelpers.TryGetValue(out var result))
        {
            return new(result);
        }

        return new(_lazyTagHelpers.GetValueAsync(cancellationToken));
    }

    public RemoteRazorDocument GetDocument(DocumentId documentId)
    {
        var document = _project.GetRequiredDocument(documentId);
        return GetDocument(document);
    }

    public RemoteRazorDocument GetDocument(TextDocument textDocument)
    {
        if (textDocument.Project != _project)
        {
            throw new ArgumentException(SR.Document_does_not_belong_to_this_project, nameof(textDocument));
        }

        if (!textDocument.IsRazorDocument())
        {
            throw new ArgumentException(SR.Document_is_not_a_Razor_document);
        }

        return GetDocumentCore(textDocument);
    }

    private RemoteRazorDocument GetDocumentCore(TextDocument textDocument)
    {
        lock (_documentMap)
        {
            if (!_documentMap.TryGetValue(textDocument, out var document))
            {
                document = new RemoteRazorDocument(textDocument, this);
                _documentMap.Add(textDocument, document);
            }

            return document;
        }
    }

    public bool ContainsDocument(string filePath)
    {
        if (!filePath.IsRazorFilePath())
        {
            throw new ArgumentException(SR.Format0_is_not_a_Razor_file_path(filePath), nameof(filePath));
        }

        var documentIds = _project.Solution.GetDocumentIdsWithFilePath(filePath);

        foreach (var documentId in documentIds)
        {
            if (_project.Id == documentId.ProjectId &&
                _project.ContainsAdditionalDocument(documentId))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGetDocument(string filePath, [NotNullWhen(true)] out IRazorDocument? document)
    {
        if (!filePath.IsRazorFilePath())
        {
            throw new ArgumentException(SR.Format0_is_not_a_Razor_file_path(filePath), nameof(filePath));
        }

        var documentIds = _project.Solution.GetDocumentIdsWithFilePath(filePath);

        foreach (var documentId in documentIds)
        {
            if (_project.Id == documentId.ProjectId &&
                _project.GetAdditionalDocument(documentId) is { } doc)
            {
                document = GetDocumentCore(doc);
                return true;
            }
        }

        document = null;
        return false;
    }

    /// <summary>
    /// NOTE: This will be removed when the source generator is used directly.
    /// </summary>
    public ValueTask<RazorProjectEngine> GetProjectEngineAsync(CancellationToken cancellationToken)
    {
        if (_lazyProjectEngine.TryGetValue(out var result))
        {
            return new(result);
        }

        return new(_lazyProjectEngine.GetValueAsync(cancellationToken));
    }

    private async Task<RazorConfiguration> ComputeConfigurationAsync(CancellationToken cancellationToken)
    {
        // See RazorSourceGenerator.RazorProviders.cs

        var globalOptions = _project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions;

        globalOptions.TryGetValue("build_property.RazorConfiguration", out var configurationName);

        configurationName ??= "MVC-3.0"; // TODO: Source generator uses "default" here??

        if (!globalOptions.TryGetValue("build_property.RazorLangVersion", out var razorLanguageVersionString) ||
            !RazorLanguageVersion.TryParse(razorLanguageVersionString, out var razorLanguageVersion))
        {
            razorLanguageVersion = RazorLanguageVersion.Latest;
        }

        var compilation = await _project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

        var suppressAddComponentParameter = compilation is not null && !compilation.HasAddComponentParameter();

        return new(
            razorLanguageVersion,
            configurationName,
            Extensions: [],
            UseConsolidatedMvcViews: true,
            suppressAddComponentParameter);
    }

    private async Task<RazorProjectEngine> ComputeProjectEngineAsync(CancellationToken cancellationToken)
    {
        var configuration = await _lazyConfiguration.GetValueAsync(cancellationToken).ConfigureAwait(false);

        var useRoslynTokenizer = SolutionSnapshot.SnapshotManager.CompilerOptions.IsFlagSet(RazorCompilerOptions.UseRoslynTokenizer);

        return ProjectEngineFactories.DefaultProvider.Create(
            configuration,
            rootDirectoryPath: Path.GetDirectoryName(FilePath).AssumeNotNull(),
            configure: builder =>
            {
                builder.SetRootNamespace(RootNamespace);
                builder.SetCSharpLanguageVersion(CSharpLanguageVersion);
                builder.SetSupportLocalizedComponentNames();
                builder.Features.Add(new ConfigureRazorParserOptions(useRoslynTokenizer, CSharpParseOptions.Default));
            });
    }

    private async Task<ImmutableArray<TagHelperDescriptor>> ComputeTagHelpersAsync(CancellationToken cancellationToken)
    {
        var projectEngine = await _lazyProjectEngine.GetValueAsync(cancellationToken).ConfigureAwait(false);
        var telemetryReporter = SolutionSnapshot.SnapshotManager.TelemetryReporter;

        return await _project.GetTagHelpersAsync(projectEngine, telemetryReporter, cancellationToken).ConfigureAwait(false);
    }
}
