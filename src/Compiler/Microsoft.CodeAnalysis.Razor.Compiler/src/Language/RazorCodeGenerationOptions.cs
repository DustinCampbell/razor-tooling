// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial record class RazorCodeGenerationOptions
{
    public static RazorCodeGenerationOptions Default { get; } = new RazorCodeGenerationOptions(
        indentSize: 4,
        newLine: Environment.NewLine,
        rootNamespace: null,
        flags: RazorCodeGenerationOptionsFlags.DefaultFlags);

    public static RazorCodeGenerationOptions DesignTimeDefault { get; } = new RazorCodeGenerationOptions(
        indentSize: 4,
        newLine: Environment.NewLine,
        rootNamespace: null,
        flags: RazorCodeGenerationOptionsFlags.DefaultDesignTimeFlags);

    private readonly RazorCodeGenerationOptionsFlags _flags;

    public int IndentSize { get; }
    public string NewLine { get; }

    /// <summary>
    /// Gets the root namespace for the generated code.
    /// </summary>
    public string? RootNamespace { get; }

    private RazorCodeGenerationOptions(
        int indentSize,
        string newLine,
        string? rootNamespace,
        RazorCodeGenerationOptionsFlags flags)
    {
        _flags = flags;
        IndentSize = indentSize;
        NewLine = newLine;
        RootNamespace = rootNamespace;
    }

    public bool DesignTime
        => _flags.IsFlagSet(RazorCodeGenerationOptionsFlags.DesignTime);

    public bool IndentWithTabs
        => _flags.IsFlagSet(RazorCodeGenerationOptionsFlags.IndentWithTabs);

    /// <summary>
    /// Gets a value that indicates whether to suppress the default <c>#pragma checksum</c> directive in the
    /// generated C# code. If <c>false</c> the checksum directive will be included, otherwise it will not be
    /// generated. Defaults to <c>false</c>, meaning that the checksum will be included.
    /// </summary>
    /// <remarks>
    /// The <c>#pragma checksum</c> is required to enable debugging and should only be suppressed for testing
    /// purposes.
    /// </remarks>
    public bool SuppressChecksum
        => _flags.IsFlagSet(RazorCodeGenerationOptionsFlags.SuppressChecksum);

    /// <summary>
    /// Gets a value that indicates whether to suppress the default metadata attributes in the generated
    /// C# code. If <c>false</c> the default attributes will be included, otherwise they will not be generated.
    /// Defaults to <c>false</c> at run time, meaning that the attributes will be included. Defaults to
    /// <c>true</c> at design time, meaning that the attributes will not be included.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>Microsoft.AspNetCore.Razor.Runtime</c> package includes a default set of attributes intended
    /// for runtimes to discover metadata about the compiled code.
    /// </para>
    /// <para>
    /// The default metadata attributes should be suppressed if code generation targets a runtime without
    /// a reference to <c>Microsoft.AspNetCore.Razor.Runtime</c>, or for testing purposes.
    /// </para>
    /// </remarks>
    public bool SuppressMetadataAttributes
        => _flags.IsFlagSet(RazorCodeGenerationOptionsFlags.SuppressMetadataAttributes);

    /// <summary>
    /// Gets a value that indicates whether to suppress the <c>RazorSourceChecksumAttribute</c>.
    /// <para>
    /// Used by default in .NET 6 apps since including a type-level attribute that changes on every
    /// edit are treated as rude edits by hot reload.
    /// </para>
    /// </summary>
    public bool SuppressMetadataSourceChecksumAttributes
        => _flags.IsFlagSet(RazorCodeGenerationOptionsFlags.SuppressMetadataSourceChecksumAttributes);

    /// <summary>
    /// Gets or sets a value that determines if an empty body is generated for the primary method.
    /// </summary>
    public bool SuppressPrimaryMethodBody
        => _flags.IsFlagSet(RazorCodeGenerationOptionsFlags.SuppressPrimaryMethodBody);

    /// <summary>
    /// Gets a value that determines if nullability type enforcement should be suppressed for user code.
    /// </summary>
    public bool SuppressNullabilityEnforcement
        => _flags.IsFlagSet(RazorCodeGenerationOptionsFlags.SuppressNullabilityEnforcement);

    /// <summary>
    /// Gets a value that determines if the components code writer may omit values for minimized attributes.
    /// </summary>
    public bool OmitMinimizedComponentAttributeValues
        => _flags.HasFlag(RazorCodeGenerationOptionsFlags.OmitMinimizedComponentAttributeValues);

    /// <summary>
    /// Gets a value that determines if localized component names are to be supported.
    /// </summary>
    public bool SupportLocalizedComponentNames
        => _flags.IsFlagSet(RazorCodeGenerationOptionsFlags.SupportLocalizedComponentNames);

    /// <summary>
    /// Gets a value that determines if enhanced line pragmas are to be utilized.
    /// </summary>
    public bool UseEnhancedLinePragma
        => _flags.IsFlagSet(RazorCodeGenerationOptionsFlags.UseEnhancedLinePragma);

    /// <summary>
    /// Determines whether RenderTreeBuilder.AddComponentParameter should not be used.
    /// </summary>
    public bool SuppressAddComponentParameter
        => _flags.IsFlagSet(RazorCodeGenerationOptionsFlags.SuppressAddComponentParameter);

    /// <summary>
    /// Determines if the file paths emitted as part of line pragmas should be mapped back to a valid path on windows.
    /// </summary>
    public bool RemapLinePragmaPathsOnWindows
        => _flags.IsFlagSet(RazorCodeGenerationOptionsFlags.RemapLinePragmaPathsOnWindows);

    internal RazorCodeGenerationOptions WithIndentSize(int indentSize)
    {
        ArgHelper.ThrowIfNegative(indentSize);

        return IndentSize == indentSize
            ? this
            : new(indentSize, NewLine, RootNamespace, _flags);
    }

    internal RazorCodeGenerationOptions WithNewLine(string newLine)
    {
        ArgHelper.ThrowIfNull(newLine);

        return NewLine == newLine
            ? this
            : new(IndentSize, newLine, RootNamespace, _flags);
    }

    internal RazorCodeGenerationOptions WithRootNamespace(string? rootNamespace)
    {
        return RootNamespace == rootNamespace
            ? this
            : new(IndentSize, NewLine, rootNamespace, _flags);
    }

    /// <summary>
    ///  Creates a new options instance with the specified flag values.
    /// </summary>
    internal RazorCodeGenerationOptions WithFlags(
        Optional<bool> designTime = default,
        Optional<bool> indentWithTabs = default,
        Optional<bool> suppressChecksum = default,
        Optional<bool> suppressMetadataAttributes = default,
        Optional<bool> suppressMetadataSourceChecksumAttributes = default,
        Optional<bool> suppressPrimaryMethodBody = default,
        Optional<bool> suppressNullabilityEnforcement = default,
        Optional<bool> omitMinimizedComponentAttributeValues = default,
        Optional<bool> supportLocalizedComponentNames = default,
        Optional<bool> useEnhancedLinePragma = default,
        Optional<bool> suppressAddComponentParameter = default,
        Optional<bool> remapLinePragmaPathsOnWindows = default)
    {
        var flags = _flags;

        if (designTime.HasValue)
        {
            flags.UpdateFlag(RazorCodeGenerationOptionsFlags.DesignTime, designTime.Value);
        }

        if (indentWithTabs.HasValue)
        {
            flags.UpdateFlag(RazorCodeGenerationOptionsFlags.IndentWithTabs, indentWithTabs.Value);
        }

        if (suppressChecksum.HasValue)
        {
            flags.UpdateFlag(RazorCodeGenerationOptionsFlags.SuppressChecksum, suppressChecksum.Value);
        }

        if (suppressMetadataAttributes.HasValue)
        {
            flags.UpdateFlag(RazorCodeGenerationOptionsFlags.SuppressMetadataAttributes, suppressMetadataAttributes.Value);
        }

        if (suppressMetadataSourceChecksumAttributes.HasValue)
        {
            flags.UpdateFlag(RazorCodeGenerationOptionsFlags.SuppressMetadataSourceChecksumAttributes, suppressMetadataSourceChecksumAttributes.Value);
        }

        if (suppressPrimaryMethodBody.HasValue)
        {
            flags.UpdateFlag(RazorCodeGenerationOptionsFlags.SuppressPrimaryMethodBody, suppressPrimaryMethodBody.Value);
        }

        if (suppressNullabilityEnforcement.HasValue)
        {
            flags.UpdateFlag(RazorCodeGenerationOptionsFlags.SuppressNullabilityEnforcement, suppressNullabilityEnforcement.Value);
        }

        if (omitMinimizedComponentAttributeValues.HasValue)
        {
            flags.UpdateFlag(RazorCodeGenerationOptionsFlags.OmitMinimizedComponentAttributeValues, omitMinimizedComponentAttributeValues.Value);
        }

        if (supportLocalizedComponentNames.HasValue)
        {
            flags.UpdateFlag(RazorCodeGenerationOptionsFlags.SupportLocalizedComponentNames, supportLocalizedComponentNames.Value);
        }

        if (useEnhancedLinePragma.HasValue)
        {
            flags.UpdateFlag(RazorCodeGenerationOptionsFlags.UseEnhancedLinePragma, useEnhancedLinePragma.Value);
        }

        if (suppressAddComponentParameter.HasValue)
        {
            flags.UpdateFlag(RazorCodeGenerationOptionsFlags.SuppressAddComponentParameter, suppressAddComponentParameter.Value);
        }

        if (remapLinePragmaPathsOnWindows.HasValue)
        {
            flags.UpdateFlag(RazorCodeGenerationOptionsFlags.RemapLinePragmaPathsOnWindows, remapLinePragmaPathsOnWindows.Value);
        }

        if (_flags == flags)
        {
            return this;
        }

        return new(IndentSize, NewLine, RootNamespace, flags);
    }

    public static RazorCodeGenerationOptions Create(Action<Builder> configure)
    {
        ArgHelper.ThrowIfNull(configure);

        var builder = new Builder(RazorLanguageVersion.Latest);
        configure(builder);
        var options = builder.ToOptions();

        return options;
    }

    public static RazorCodeGenerationOptions CreateDesignTime(Action<Builder> configure)
    {
        ArgHelper.ThrowIfNull(configure);

        var builder = new Builder(RazorLanguageVersion.Latest)
        {
            DesignTime = true,
            SuppressMetadataAttributes = true
        };

        configure(builder);

        return builder.ToOptions();
    }
}
