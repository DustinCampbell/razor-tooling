// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal abstract class MockBuilder<T> : IMockBuilder<T>
    where T : class
{
    public StrictMock<T> Mock { get; }

    protected MockBuilder()
    {
        Mock = new();
    }
}

internal interface IMockBuilder<T>
    where T : class
{
    StrictMock<T> Mock { get; }
}
