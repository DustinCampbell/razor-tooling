// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Moq;
using Moq.Language;
using Moq.Language.Flow;

namespace Microsoft.AspNetCore.Razor.Test.Common;

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

internal static class MockExtensions
{
    public static IReturnsResult<TMock> ReturnsNull<TMock, TResult>(this IReturns<TMock, TResult?> mock)
        where TMock : class
        where TResult : class
        => mock.Returns<TResult?>(null);

    public static IReturnsResult<TMock> ReturnsNull<TMock, TResult>(this IReturns<TMock, TResult?> mock)
        where TMock : class
        where TResult : struct
        => mock.Returns<TResult?>(null);

    public static IReturnsResult<TMock> ReturnsNullAsync<TMock, TResult>(this IReturns<TMock, Task<TResult?>> mock)
        where TMock : class
        where TResult : class
        => mock.ReturnsAsync((TResult?)null);

    public static IReturnsResult<TMock> ReturnsNullAsync<TMock, TResult>(this IReturns<TMock, Task<TResult?>> mock)
        where TMock : class
        where TResult : struct
        => mock.ReturnsAsync((TResult?)null);

    public static IReturnsResult<TMock> ReturnsNullAsync<TMock, TResult>(this IReturns<TMock, ValueTask<TResult?>> mock)
        where TMock : class
        where TResult : class
        => mock.ReturnsAsync((TResult?)null);

    public static IReturnsResult<TMock> ReturnsNullAsync<TMock, TResult>(this IReturns<TMock, ValueTask<TResult?>> mock)
        where TMock : class
        where TResult : struct
        => mock.ReturnsAsync((TResult?)null);

    public static IReturnsResult<TMock> ReturnsAsync<TMock>(this IReturns<TMock, Task> mock)
        where TMock : class
        => mock.Returns(Task.CompletedTask);

    public static IReturnsResult<TMock> ReturnsAsync<TMock>(this IReturns<TMock, ValueTask> mock)
        where TMock : class
        => mock.Returns<ValueTask>(default);
}
