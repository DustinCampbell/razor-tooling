// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal static class TagHelperBoundAttributeDescriptorExtensions
{
    private static bool IsTrue(this BoundAttributeDescriptor attribute, string key)
        => attribute.Metadata.TryGetValue(key, out var value) && value == bool.TrueString;

    public static bool IsDelegateProperty(this BoundAttributeDescriptor attribute)
        => attribute.IsTrue(ComponentMetadata.Component.DelegateSignatureKey);

    public static bool IsDelegateWithAwaitableResult(this BoundAttributeDescriptor attribute)
        => attribute.IsTrue(ComponentMetadata.Component.DelegateWithAwaitableResultKey);

    public static bool IsGenericTypedProperty(this BoundAttributeDescriptor attribute)
        => attribute.IsTrue(ComponentMetadata.Component.GenericTypedKey);

    public static bool IsTypeParameterProperty(this BoundAttributeDescriptor attribute)
        => attribute.IsTrue(ComponentMetadata.Component.TypeParameterKey);

    public static bool IsCascadingTypeParameterProperty(this BoundAttributeDescriptor attribute)
        => attribute.IsTrue(ComponentMetadata.Component.TypeParameterIsCascadingKey);

    /// <summary>
    /// Gets a value that indicates whether the property is a parameterized child content property. Properties are
    /// considered parameterized child content if they have the type <c>RenderFragment{T}</c> (for some T).
    /// </summary>
    /// <param name="attribute">The <see cref="BoundAttributeDescriptor"/>.</param>
    /// <returns>Returns <c>true</c> if the property is parameterized child content, otherwise <c>false</c>.</returns>
    public static bool IsParameterizedChildContentProperty(this BoundAttributeDescriptor attribute)
        => attribute.IsChildContentProperty &&
           attribute.TypeName != ComponentsApi.RenderFragment.FullTypeName;

    /// <summary>
    /// Gets a value that indicates whether the property is a parameterized child content property. Properties are
    /// considered parameterized child content if they have the type <c>RenderFragment{T}</c> (for some T).
    /// </summary>
    /// <param name="attribute">The <see cref="BoundAttributeDescriptor"/>.</param>
    /// <returns>Returns <c>true</c> if the property is parameterized child content, otherwise <c>false</c>.</returns>
    public static bool IsParameterizedChildContentProperty(this BoundAttributeDescriptorBuilder attribute)
        => attribute.IsChildContentProperty &&
           attribute.TypeName != ComponentsApi.RenderFragment.FullTypeName;
}
