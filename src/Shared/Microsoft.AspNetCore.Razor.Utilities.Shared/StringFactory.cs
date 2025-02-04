// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor;

internal static class StringFactory
{
    /// <summary>
    ///  Encapsulates a method that receives a span of objects of type <typeparamref name="T"/>
    ///  and a state object of type <typeparamref name="TArg"/>.
    /// </summary>
    /// <typeparam name="T">
    ///  The type of the objects in the span.
    /// </typeparam>
    /// <typeparam name="TArg">
    ///  The type of the object that represents the state.
    /// </typeparam>
    /// <param name="span">
    ///  A span of objects of type <typeparamref name="T"/>.
    /// </param>
    /// <param name="arg">
    ///  A state object of type <typeparamref name="TArg"/>.
    /// </param>
    public delegate void SpanAction<T, in TArg>(Span<T> span, TArg arg);

    /// <summary>
    ///  Creates a new string with a specific length and initializes it after creation by using the specified callback.
    /// </summary>
    /// <typeparam name="TState">
    ///  The type of the element to pass to <paramref name="action"/>.
    /// </typeparam>
    /// <param name="length">
    ///  The length of the string to create.
    /// </param>
    /// <param name="state">
    ///  The element to pass to <paramref name="action"/>.
    /// </param>
    /// <param name="action">
    ///  A callback to initialize the string
    /// </param>
    /// <returns>
    ///  The created string.
    /// </returns>
    /// <remarks>
    ///  The initial content of the destination span passed to <paramref name="action"/> is undefined.
    ///  Therefore, it is the delegate's responsibility to ensure that every element of the span is assigned.
    ///  Otherwise, the resulting string could contain random characters
    /// </remarks>
    public unsafe static string Create<TState>(int length, TState state, SpanAction<char, TState> action)
    {
#if NET
        return string.Create(length, (action, state), static (span, state) => state.action(span, state.state));
#else
        ArgHelper.ThrowIfNegative(length);

        if (length == 0)
        {
            return string.Empty;
        }

        var result = new string('\0', length);

        fixed (char* ptr = result)
        {
            action(new Span<char>(ptr, length), state);
        }

        return result;
#endif
    }
}
