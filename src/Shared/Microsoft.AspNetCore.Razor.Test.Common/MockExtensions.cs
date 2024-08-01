// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq.Expressions;
using Moq;
using Moq.Language.Flow;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal static class MockExtensions
{
    public static ICallbackResult Setup<TMock, T>(
        this Mock<TMock> mock,
        Expression<Action<TMock>> expression,
        out T arg)
        where TMock : class
    {
        T capture = default!;

        var result = mock
            .Setup(expression)
            .Callback((T a) =>
            {
                capture = a;
            });

        arg = capture;

        return result;
    }

    public static ICallbackResult Setup<TMock, T1, T2>(
        this Mock<TMock> mock,
        Expression<Action<TMock>> expression,
        out T1 arg1, out T2 arg2)
        where TMock : class
    {
        T1 capture1 = default!;
        T2 capture2 = default!;

        var result = mock
            .Setup(expression)
            .Callback((T1 a1, T2 a2) =>
            {
                capture1 = a1;
                capture2 = a2;
            });

        arg1 = capture1;
        arg2 = capture2;

        return result;
    }

    public static ICallbackResult Setup<TMock, T1, T2, T3>(
        this Mock<TMock> mock,
        Expression<Action<TMock>> expression,
        out T1 arg1, out T2 arg2, out T3 arg3)
        where TMock : class
    {
        T1 capture1 = default!;
        T2 capture2 = default!;
        T3 capture3 = default!;

        var result = mock
            .Setup(expression)
            .Callback((T1 a1, T2 a2, T3 a3) =>
            {
                capture1 = a1;
                capture2 = a2;
                capture3 = a3;
            });

        arg1 = capture1;
        arg2 = capture2;
        arg3 = capture3;

        return result;
    }

    public static ICallbackResult Setup<TMock, T1, T2, T3, T4>(
        this Mock<TMock> mock,
        Expression<Action<TMock>> expression,
        out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4)
        where TMock : class
    {
        T1 capture1 = default!;
        T2 capture2 = default!;
        T3 capture3 = default!;
        T4 capture4 = default!;

        var result = mock
            .Setup(expression)
            .Callback((T1 a1, T2 a2, T3 a3, T4 a4) =>
            {
                capture1 = a1;
                capture2 = a2;
                capture3 = a3;
                capture4 = a4;
            });

        arg1 = capture1;
        arg2 = capture2;
        arg3 = capture3;
        arg4 = capture4;

        return result;
    }

    public static IReturnsThrows<TMock, TResult> Setup<TMock, T, TResult>(
        this Mock<TMock> mock,
        Expression<Func<TMock, TResult>> expression,
        out T arg)
        where TMock : class
    {
        T capture = default!;

        var result = mock
            .Setup(expression)
            .Callback((T a) =>
            {
                capture = a;
            });

        arg = capture;

        return result;
    }

    public static IReturnsThrows<TMock, TResult> Setup<TMock, T1, T2, TResult>(
        this Mock<TMock> mock,
        Expression<Func<TMock, TResult>> expression,
        out T1 arg1, out T2 arg2)
        where TMock : class
    {
        T1 capture1 = default!;
        T2 capture2 = default!;

        var result = mock
            .Setup(expression)
            .Callback((T1 a1, T2 a2) =>
            {
                capture1 = a1;
                capture2 = a2;
            });

        arg1 = capture1;
        arg2 = capture2;

        return result;
    }

    public static IReturnsThrows<TMock, TResult> Setup<TMock, T1, T2, T3, TResult>(
        this Mock<TMock> mock,
        Expression<Func<TMock, TResult>> expression,
        out T1 arg1, out T2 arg2, out T3 arg3)
        where TMock : class
    {
        T1 capture1 = default!;
        T2 capture2 = default!;
        T3 capture3 = default!;

        var result = mock
            .Setup(expression)
            .Callback((T1 a1, T2 a2, T3 a3) =>
            {
                capture1 = a1;
                capture2 = a2;
                capture3 = a3;
            });

        arg1 = capture1;
        arg2 = capture2;
        arg3 = capture3;

        return result;
    }

    public static IReturnsThrows<TMock, TResult> Setup<TMock, T1, T2, T3, T4, TResult>(
        this Mock<TMock> mock,
        Expression<Func<TMock, TResult>> expression,
        out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4)
        where TMock : class
    {
        T1 capture1 = default!;
        T2 capture2 = default!;
        T3 capture3 = default!;
        T4 capture4 = default!;

        var result = mock
            .Setup(expression)
            .Callback((T1 a1, T2 a2, T3 a3, T4 a4) =>
            {
                capture1 = a1;
                capture2 = a2;
                capture3 = a3;
                capture4 = a4;
            });

        arg1 = capture1;
        arg2 = capture2;
        arg3 = capture3;
        arg4 = capture4;

        return result;
    }

    public static ICallbackResult Callback<TMock, T>(
        this ISetup<TMock> setup,
        Action<T> action,
        out T arg)
        where TMock : class
    {
        T capture = default!;

        var result = setup.Callback((T a) =>
        {
            action(a);
            capture = a;
        });

        arg = capture;

        return result;
    }

    public static ICallbackResult Callback<TMock, T1, T2>(
        this ISetup<TMock> setup,
        Action<T1, T2> action,
        out T1 arg1, out T2 arg2)
        where TMock : class
    {
        T1 capture1 = default!;
        T2 capture2 = default!;

        var result = setup.Callback((T1 a1, T2 a2) =>
        {
            action(a1, a2);
            capture1 = a1;
            capture2 = a2;
        });

        arg1 = capture1;
        arg2 = capture2;

        return result;
    }

    public static ICallbackResult Callback<TMock, T1, T2, T3>(
        this ISetup<TMock> setup,
        Action<T1, T2, T3> action,
        out T1 arg1, out T2 arg2, out T3 arg3)
        where TMock : class
    {
        T1 capture1 = default!;
        T2 capture2 = default!;
        T3 capture3 = default!;

        var result = setup.Callback((T1 a1, T2 a2, T3 a3) =>
        {
            action(a1, a2, a3);
            capture1 = a1;
            capture2 = a2;
            capture3 = a3;
        });

        arg1 = capture1;
        arg2 = capture2;
        arg3 = capture3;

        return result;
    }

    public static ICallbackResult Callback<TMock, T1, T2, T3, T4>(
        this ISetup<TMock> setup,
        Action<T1, T2, T3, T4> action,
        out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4)
        where TMock : class
    {
        T1 capture1 = default!;
        T2 capture2 = default!;
        T3 capture3 = default!;
        T4 capture4 = default!;

        var result = setup.Callback((T1 a1, T2 a2, T3 a3, T4 a4) =>
        {
            action(a1, a2, a3, a4);
            capture1 = a1;
            capture2 = a2;
            capture3 = a3;
            capture4 = a4;
        });

        arg1 = capture1;
        arg2 = capture2;
        arg3 = capture3;
        arg4 = capture4;

        return result;
    }

    public static IReturnsThrows<TMock, TResult> Callback<TMock, T1, TResult>(
        this ISetup<TMock, TResult> setup,
        Action<T1> action,
        out T1 arg)
        where TMock : class
    {
        T1 capture = default!;

        var result = setup.Callback((T1 a) =>
        {
            action(a);
            capture = a;
        });

        arg = capture;

        return result;
    }

    public static IReturnsThrows<TMock, TResult> Callback<TMock, T1, T2, TResult>(
        this ISetup<TMock, TResult> setup,
        Action<T1, T2> action,
        out T1 arg1, out T2 arg2)
        where TMock : class
    {
        T1 capture1 = default!;
        T2 capture2 = default!;

        var result = setup.Callback((T1 a1, T2 a2) =>
        {
            action(a1, a2);
            capture1 = a1;
            capture2 = a2;
        });

        arg1 = capture1;
        arg2 = capture2;

        return result;
    }

    public static IReturnsThrows<TMock, TResult> Callback<TMock, T1, T2, T3, TResult>(
        this ISetup<TMock, TResult> setup,
        Action<T1, T2, T3> action,
        out T1 arg1, out T2 arg2, out T3 arg3)
        where TMock : class
    {
        T1 capture1 = default!;
        T2 capture2 = default!;
        T3 capture3 = default!;

        var result = setup.Callback((T1 a1, T2 a2, T3 a3) =>
        {
            action(a1, a2, a3);
            capture1 = a1;
            capture2 = a2;
            capture3 = a3;
        });

        arg1 = capture1;
        arg2 = capture2;
        arg3 = capture3;

        return result;
    }

    public static IReturnsThrows<TMock, TResult> Callback<TMock, T1, T2, T3, T4, TResult>(
        this ISetup<TMock, TResult> setup,
        Action<T1, T2, T3, T4> action,
        out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4)
        where TMock : class
    {
        T1 capture1 = default!;
        T2 capture2 = default!;
        T3 capture3 = default!;
        T4 capture4 = default!;

        var result = setup.Callback((T1 a1, T2 a2, T3 a3, T4 a4) =>
        {
            action(a1, a2, a3, a4);
            capture1 = a1;
            capture2 = a2;
            capture3 = a3;
            capture4 = a4;
        });

        arg1 = capture1;
        arg2 = capture2;
        arg3 = capture3;
        arg4 = capture4;

        return result;
    }

    public static ICallbackResult CaptureArgs<TMock, T1>(
        this ISetup<TMock> setup,
        out T1 arg)
        where TMock : class
    {
        T1 capture = default!;

        var result = setup.Callback((T1 a) => capture = a);

        arg = capture;
        return result;
    }

    public static ICallbackResult CaptureArgs<TMock, T1, T2>(
        this ISetup<TMock> setup,
        out T1 arg1, out T2 arg2)
        where TMock : class
    {
        T1 capture1 = default!;
        T2 capture2 = default!;

        var result = setup.Callback((T1 a1, T2 a2) =>
        {
            capture1 = a1;
            capture2 = a2;
        });

        arg1 = capture1;
        arg2 = capture2;

        return result;
    }

    public static ICallbackResult CaptureArgs<TMock, T1, T2, T3>(
        this ISetup<TMock> setup,
        out T1 arg1, out T2 arg2, out T3 arg3)
        where TMock : class
    {
        T1 capture1 = default!;
        T2 capture2 = default!;
        T3 capture3 = default!;

        var result = setup.Callback((T1 a1, T2 a2, T3 a3) =>
        {
            capture1 = a1;
            capture2 = a2;
            capture3 = a3;
        });

        arg1 = capture1;
        arg2 = capture2;
        arg3 = capture3;

        return result;
    }

    public static ICallbackResult CaptureArgs<TMock, T1, T2, T3, T4>(
        this ISetup<TMock> setup,
        out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4)
        where TMock : class
    {
        T1 capture1 = default!;
        T2 capture2 = default!;
        T3 capture3 = default!;
        T4 capture4 = default!;

        var result = setup.Callback((T1 a1, T2 a2, T3 a3, T4 a4) =>
        {
            capture1 = a1;
            capture2 = a2;
            capture3 = a3;
            capture4 = a4;
        });

        arg1 = capture1;
        arg2 = capture2;
        arg3 = capture3;
        arg4 = capture4;

        return result;
    }

    public static IReturnsThrows<TMock, TResult> CaptureArgs<TMock, T1, TResult>(
        this ISetup<TMock, TResult> setup,
        out T1 arg)
        where TMock : class
    {
        T1 capture = default!;

        var result = setup.Callback((T1 a) => capture = a);

        arg = capture;

        return result;
    }

    public static IReturnsThrows<TMock, TResult> CaptureArgs<TMock, T1, T2, TResult>(
        this ISetup<TMock, TResult> setup,
        out T1 arg1, out T2 arg2)
        where TMock : class
    {
        T1 capture1 = default!;
        T2 capture2 = default!;

        var result = setup.Callback((T1 a1, T2 a2) =>
        {
            capture1 = a1;
            capture2 = a2;
        });

        arg1 = capture1;
        arg2 = capture2;

        return result;
    }

    public static IReturnsThrows<TMock, TResult> CaptureArgs<TMock, T1, T2, T3, TResult>(
        this ISetup<TMock, TResult> setup,
        out T1 arg1, out T2 arg2, out T3 arg3)
        where TMock : class
    {
        T1 capture1 = default!;
        T2 capture2 = default!;
        T3 capture3 = default!;

        var result = setup.Callback((T1 a1, T2 a2, T3 a3) =>
        {
            capture1 = a1;
            capture2 = a2;
            capture3 = a3;
        });

        arg1 = capture1;
        arg2 = capture2;
        arg3 = capture3;

        return result;
    }

    public static IReturnsThrows<TMock, TResult> CaptureArgs<TMock, T1, T2, T3, T4, TResult>(
        this ISetup<TMock, TResult> setup,
        out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4)
        where TMock : class
    {
        T1 capture1 = default!;
        T2 capture2 = default!;
        T3 capture3 = default!;
        T4 capture4 = default!;

        var result = setup.Callback((T1 a1, T2 a2, T3 a3, T4 a4) =>
        {
            capture1 = a1;
            capture2 = a2;
            capture3 = a3;
            capture4 = a4;
        });

        arg1 = capture1;
        arg2 = capture2;
        arg3 = capture3;
        arg4 = capture4;

        return result;
    }
}
