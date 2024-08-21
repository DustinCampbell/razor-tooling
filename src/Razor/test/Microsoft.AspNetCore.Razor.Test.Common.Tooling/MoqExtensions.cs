// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Moq.Language;
using Moq.Language.Flow;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal static class MoqExtensions
{
    public static IReturnsResult<T> ReturnsNull<T, TResult>(this IReturns<T, TResult?> returns)
        where T : class
        where TResult : class
    {
        return returns.Returns((TResult?)null);
    }

    public static IReturnsResult<T> ReturnsNull<T, TResult>(this IReturns<T, TResult?> returns)
        where T : class
        where TResult : struct
    {
        return returns.Returns((TResult?)null);
    }

    public static IReturnsResult<T> ReturnsValue<T, TResult>(this IReturns<T, TResult?> returns, TResult value)
        where T : class
    {
        return returns.Returns(value: value);
    }
}
