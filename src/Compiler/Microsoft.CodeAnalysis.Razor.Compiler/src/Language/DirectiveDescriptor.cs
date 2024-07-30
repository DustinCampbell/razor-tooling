// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// A descriptor type for a directive that can be parsed by the Razor parser.
/// </summary>
public sealed class DirectiveDescriptor
{
    /// <summary>
    /// Gets the description of the directive.
    /// </summary>
    /// <remarks>
    /// The description is used for information purposes, and has no effect on parsing.
    /// </remarks>
    public string Description { get; }

    /// <summary>
    /// Gets the directive keyword without the leading <c>@</c> token.
    /// </summary>
    public string Directive { get; }

    /// <summary>
    /// Gets the display name of the directive.
    /// </summary>
    /// <remarks>
    /// The display name is used for information purposes, and has no effect on parsing.
    /// </remarks>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the kind of the directive. The kind determines whether or not a directive has an associated block.
    /// </summary>
    public DirectiveKind Kind { get; }

    /// <summary>
    /// Gets the way a directive can be used. The usage determines how many, and where directives can exist per document.
    /// </summary>
    public DirectiveUsage Usage { get; }

    /// <summary>
    /// Gets the list of directive tokens that can follow the directive keyword.
    /// </summary>
    public ImmutableArray<DirectiveTokenDescriptor> Tokens { get; }

    private DirectiveDescriptor(
        string directive,
        DirectiveKind kind,
        DirectiveUsage usage,
        ImmutableArray<DirectiveTokenDescriptor> tokens,
        string displayName,
        string description)
    {
        Directive = directive;
        Kind = kind;
        Usage = usage;
        Tokens = tokens;
        DisplayName = displayName;
        Description = description;
    }

    /// <summary>
    /// Creates a new <see cref="DirectiveDescriptor"/>.
    /// </summary>
    /// <param name="directive">The directive keyword.</param>
    /// <param name="kind">The directive kind.</param>
    /// <param name="configure">A configuration delegate for the directive.</param>
    /// <returns>A <see cref="DirectiveDescriptor"/> for the created directive.</returns>
    public static DirectiveDescriptor CreateDirective(string directive, DirectiveKind kind, Action<IDirectiveDescriptorBuilder>? configure = null)
    {
        ArgHelper.ThrowIfNull(directive);

        using var builder = new Builder(directive, kind);
        configure?.Invoke(builder);
        return builder.Build();
    }

    /// <summary>
    /// Creates a new <see cref="DirectiveDescriptor"/> with <see cref="Kind"/> set to <see cref="DirectiveKind.SingleLine"/>
    /// </summary>
    /// <param name="directive">The directive keyword.</param>
    /// <param name="configure">A configuration delegate for the directive.</param>
    /// <returns>A <see cref="DirectiveDescriptor"/> for the created directive.</returns>
    public static DirectiveDescriptor CreateSingleLineDirective(string directive, Action<IDirectiveDescriptorBuilder>? configure = null)
    {
        ArgHelper.ThrowIfNull(directive);

        return CreateDirective(directive, DirectiveKind.SingleLine, configure);
    }

    /// <summary>
    /// Creates a new <see cref="DirectiveDescriptor"/> with <see cref="Kind"/> set to <see cref="DirectiveKind.RazorBlock"/>
    /// </summary>
    /// <param name="directive">The directive keyword.</param>
    /// <param name="configure">A configuration delegate for the directive.</param>
    /// <returns>A <see cref="DirectiveDescriptor"/> for the created directive.</returns>
    public static DirectiveDescriptor CreateRazorBlockDirective(string directive, Action<IDirectiveDescriptorBuilder>? configure = null)
    {
        ArgHelper.ThrowIfNull(directive);

        return CreateDirective(directive, DirectiveKind.RazorBlock, configure);
    }

    /// <summary>
    /// Creates a new <see cref="DirectiveDescriptor"/> with <see cref="Kind"/> set to <see cref="DirectiveKind.CodeBlock"/>
    /// </summary>
    /// <param name="directive">The directive keyword.</param>
    /// <param name="configure">A configuration delegate for the directive.</param>
    /// <returns>A <see cref="DirectiveDescriptor"/> for the created directive.</returns>
    public static DirectiveDescriptor CreateCodeBlockDirective(string directive, Action<IDirectiveDescriptorBuilder>? configure = null)
    {
        ArgHelper.ThrowIfNull(directive);

        return CreateDirective(directive, DirectiveKind.CodeBlock, configure);
    }

    private sealed class Builder(string name, DirectiveKind kind) : IDirectiveDescriptorBuilder, IDisposable
    {
        public string Name { get; } = name;
        public DirectiveKind Kind { get; } = kind;

        public string? Description { get; set; }
        public string? DisplayName { get; set; }
        public DirectiveUsage Usage { get; set; }

        public ImmutableArray<DirectiveTokenDescriptor>.Builder Tokens { get; } = ArrayBuilderPool<DirectiveTokenDescriptor>.Default.Get();

        public void Dispose()
        {
            ArrayBuilderPool<DirectiveTokenDescriptor>.Default.Return(Tokens);
        }

        public DirectiveDescriptor Build()
        {
            if (Name.Length == 0)
            {
                return ThrowHelper.ThrowInvalidOperationException<DirectiveDescriptor>(Resources.FormatDirectiveDescriptor_InvalidDirectiveKeyword(Name));
            }

            foreach (var ch in Name)
            {
                if (!char.IsLetter(ch))
                {
                    return ThrowHelper.ThrowInvalidOperationException<DirectiveDescriptor>(Resources.FormatDirectiveDescriptor_InvalidDirectiveKeyword(Name));
                }
            }

            var foundOptionalToken = false;

            for (var i = 0; i < Tokens.Count; i++)
            {
                var token = Tokens[i];
                foundOptionalToken |= token.Optional;

                if (foundOptionalToken && !token.Optional)
                {
                    return ThrowHelper.ThrowInvalidOperationException<DirectiveDescriptor>(Resources.DirectiveDescriptor_InvalidNonOptionalToken);
                }
            }

            return new(Name, Kind, Usage, Tokens.DrainToImmutable(), DisplayName!, Description!);
        }
    }
}
