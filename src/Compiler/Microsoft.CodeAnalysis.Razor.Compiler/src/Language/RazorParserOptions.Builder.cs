// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial record class RazorParserOptions
{
    public sealed class Builder
    {
        private RazorParserOptionsFlags _flags;
        private ImmutableArray<DirectiveDescriptor> _directives;

        public RazorLanguageVersion LanguageVersion { get; }
        public string FileKind { get; }
        public CSharpParseOptions CSharpParseOptions { get; set; }

        internal Builder(RazorLanguageVersion version, string fileKind)
        {
            LanguageVersion = version ?? DefaultLanguageVersion;
            FileKind = fileKind ?? DefaultFileKind;
            CSharpParseOptions = DefaultCSharpParseOptions;

            _flags = GetDefaultFlags(LanguageVersion, FileKind);
        }

        public ImmutableArray<DirectiveDescriptor> Directives
        {
            get => _directives;
            set => _directives = value.NullToEmpty();
        }

        public bool DesignTime
        {
            get => _flags.IsFlagSet(RazorParserOptionsFlags.DesignTime);
            set => _flags.UpdateFlag(RazorParserOptionsFlags.DesignTime, value);
        }

        public bool ParseLeadingDirectives
        {
            get => _flags.IsFlagSet(RazorParserOptionsFlags.ParseLeadingDirectives);
            set => _flags.UpdateFlag(RazorParserOptionsFlags.ParseLeadingDirectives, value);
        }

        public bool UseRoslynTokenizer
        {
            get => _flags.IsFlagSet(RazorParserOptionsFlags.UseRoslynTokenizer);
            set => _flags.UpdateFlag(RazorParserOptionsFlags.UseRoslynTokenizer, value);
        }

        internal bool EnableSpanEditHandlers
        {
            get => _flags.IsFlagSet(RazorParserOptionsFlags.EnableSpanEditHandlers);
            set => _flags.UpdateFlag(RazorParserOptionsFlags.EnableSpanEditHandlers, value);
        }

        internal bool AllowMinimizedBooleanTagHelperAttributes
        {
            get => _flags.IsFlagSet(RazorParserOptionsFlags.AllowMinimizedBooleanTagHelperAttributes);
            set => _flags.UpdateFlag(RazorParserOptionsFlags.AllowMinimizedBooleanTagHelperAttributes, value);
        }

        internal bool AllowHtmlCommentsInTagHelpers
        {
            get => _flags.IsFlagSet(RazorParserOptionsFlags.AllowHtmlCommentsInTagHelpers);
            set => _flags.UpdateFlag(RazorParserOptionsFlags.AllowHtmlCommentsInTagHelpers, value);
        }

        internal bool AllowComponentFileKind
        {
            get => _flags.IsFlagSet(RazorParserOptionsFlags.AllowComponentFileKind);
            set => _flags.UpdateFlag(RazorParserOptionsFlags.AllowComponentFileKind, value);
        }

        internal bool AllowRazorInAllCodeBlocks
        {
            get => _flags.IsFlagSet(RazorParserOptionsFlags.AllowRazorInAllCodeBlocks);
            set => _flags.UpdateFlag(RazorParserOptionsFlags.AllowRazorInAllCodeBlocks, value);
        }

        internal bool AllowUsingVariableDeclarations
        {
            get => _flags.IsFlagSet(RazorParserOptionsFlags.AllowUsingVariableDeclarations);
            set => _flags.UpdateFlag(RazorParserOptionsFlags.AllowUsingVariableDeclarations, value);
        }

        internal bool AllowConditionalDataDashAttributes
        {
            get => _flags.IsFlagSet(RazorParserOptionsFlags.AllowConditionalDataDashAttributes);
            set => _flags.UpdateFlag(RazorParserOptionsFlags.AllowConditionalDataDashAttributes, value);
        }

        internal bool AllowCSharpInMarkupAttributeArea
        {
            get => _flags.IsFlagSet(RazorParserOptionsFlags.AllowCSharpInMarkupAttributeArea);
            set => _flags.UpdateFlag(RazorParserOptionsFlags.AllowCSharpInMarkupAttributeArea, value);
        }

        internal bool AllowNullableForgivenessOperator
        {
            get => _flags.IsFlagSet(RazorParserOptionsFlags.AllowNullableForgivenessOperator);
            set => _flags.UpdateFlag(RazorParserOptionsFlags.AllowNullableForgivenessOperator, value);
        }

        public RazorParserOptions ToOptions()
            => new(LanguageVersion, FileKind, _directives, CSharpParseOptions, _flags);
    }
}
