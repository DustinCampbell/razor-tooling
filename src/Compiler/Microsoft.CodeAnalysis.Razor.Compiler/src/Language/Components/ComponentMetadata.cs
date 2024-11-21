// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

namespace Microsoft.AspNetCore.Razor.Language.Components;

// Metadata used for Components interactions with the tag helper system
internal static class ComponentMetadata
{
    private const string MangledClassNamePrefix = "__generated__";

    public const string ImportsFileName = "_Imports.razor";

    public static string MangleClassName(string className)
    {
        if (string.IsNullOrEmpty(className))
        {
            return string.Empty;
        }

        return MangledClassNamePrefix + className;
    }

    public static bool IsMangledClass(string className)
    {
        return className?.StartsWith(MangledClassNamePrefix, StringComparison.Ordinal) == true;
    }

    public static class Common
    {
        public const string OriginalAttributeName = "Common.OriginalAttributeName";

        public const string AddAttributeMethodName = "Common.AddAttributeMethodName";

        public const string OriginalAttributeSpan = "Common.OriginalAttributeSpan";

        public const string IsDesignTimePropertyAccessHelper = "Common.IsDesignTimePropertyAccessHelper";
    }

    public static class Bind
    {
        public const string TypeAttribute = "Components.Bind.TypeAttribute";

        public const string ValueAttribute = "Components.Bind.ValueAttribute";

        public const string ChangeAttribute = "Components.Bind.ChangeAttribute";

        public const string ExpressionAttribute = "Components.Bind.ExpressionAttribute";

        public const string IsInvariantCulture = "Components.Bind.IsInvariantCulture";

        public const string Format = "Components.Bind.Format";

        /// <summary>
        /// Represents the sub-span of the bind node that actually represents the property
        /// </summary>
        /// <remarks>
        /// <pre>
        /// @bind-Value:get=""
        /// ^----------------^ Regular node span
        ///       ^---^        Property span
        /// </pre>
        /// </remarks>
        public const string PropertySpan = "Components.Bind.PropertySpan";

        /// <summary>
        /// Used to track if this node was synthesized by the compiler and
        /// not explicitly written by a user.
        /// </summary>
        public const string IsSynthesized = "Components.Bind.IsSynthesized";
    }

    public static class ChildContent
    {
        public const string ParameterNameBoundAttributeKind = "Components.ChildContentParameterName";

        /// <summary>
        /// The name of the synthesized attribute used to set a child content parameter.
        /// </summary>
        public const string ParameterAttributeName = "Context";

        /// <summary>
        /// The default name of the child content parameter (unless set by a Context attribute).
        /// </summary>
        public const string DefaultParameterName = "context";
    }

    public static class Component
    {
        public const string ChildContentParameterNameKey = "Components.ChildContentParameterName";

        public const string DelegateSignatureKey = "Components.DelegateSignature";

        public const string DelegateWithAwaitableResultKey = "Components.IsDelegateAwaitableResult";

        public const string EventCallbackKey = "Components.EventCallback";

        public const string GenericTypedKey = "Components.GenericTyped";

        public const string ExplicitTypeNameKey = "Components.ExplicitTypeName";

        public const string OpenGenericKey = "Components.OpenGeneric";

        public const string TypeParameterKey = "Components.TypeParameter";

        public const string TypeParameterIsCascadingKey = "Components.TypeParameterIsCascading";

        public const string TypeParameterConstraintsKey = "Component.TypeParameterConstraints";

        public const string HasRenderModeDirectiveKey = "Components.HasRenderModeDirective";

        public const string InitOnlyProperty = "Components.InitOnlyProperty";

        /// <summary>
        /// When a generic component is re-written with its concrete implementation type
        /// We use this metadata on its bound attributes to track the updated type.
        /// </summary>
        public const string ConcreteContainingType = "Components.ConcreteContainingType";
    }

    public static class EventHandler
    {
        public const string EventArgsType = "Components.EventHandler.EventArgs";
    }
}
