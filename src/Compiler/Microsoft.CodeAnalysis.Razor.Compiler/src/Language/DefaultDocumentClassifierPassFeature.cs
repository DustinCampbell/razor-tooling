// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class DefaultDocumentClassifierPassFeature(
    ImmutableArray<Action<RazorCodeDocument, ClassDeclarationIntermediateNode>> configureClass,
    ImmutableArray<Action<RazorCodeDocument, NamespaceDeclarationIntermediateNode>> configureNamespace,
    ImmutableArray<Action<RazorCodeDocument, MethodDeclarationIntermediateNode>> configureMethod) : RazorEngineFeatureBase
{
    private static readonly ImmutableArray<Action<RazorCodeDocument, ClassDeclarationIntermediateNode>> s_configureClass = [DefaultConfigureClass];
    private static readonly ImmutableArray<Action<RazorCodeDocument, NamespaceDeclarationIntermediateNode>> s_configureNamespace = [DefaultConfigureNamespace];
    private static readonly ImmutableArray<Action<RazorCodeDocument, MethodDeclarationIntermediateNode>> s_configureMethod = [DefaultConfigureMethod];

    private static void DefaultConfigureClass(RazorCodeDocument document, ClassDeclarationIntermediateNode @class)
    {
        @class.ClassName = "Template";
        @class.Modifiers.Add("public");
    }

    private static void DefaultConfigureNamespace(RazorCodeDocument document, NamespaceDeclarationIntermediateNode @namespace)
    {
        @namespace.Content = "Razor";
    }

    private static void DefaultConfigureMethod(RazorCodeDocument document, MethodDeclarationIntermediateNode method)
    {
        method.MethodName = "ExecuteAsync";
        method.ReturnType = $"global::{typeof(Task).FullName}";

        method.Modifiers.Add("public");
        method.Modifiers.Add("async");
        method.Modifiers.Add("override");
    }

    public static DefaultDocumentClassifierPassFeature CreateDefault()
        => new(s_configureClass, s_configureNamespace, s_configureMethod);

    public ImmutableArray<Action<RazorCodeDocument, ClassDeclarationIntermediateNode>> ConfigureClass { get; } = configureClass;
    public ImmutableArray<Action<RazorCodeDocument, NamespaceDeclarationIntermediateNode>> ConfigureNamespace { get; } = configureNamespace;
    public ImmutableArray<Action<RazorCodeDocument, MethodDeclarationIntermediateNode>> ConfigureMethod { get; } = configureMethod;
}
