// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Razor.Diagnostics.Analyzers.Test.CSharpAnalyzerVerifier<Razor.Diagnostics.Analyzers.PooledArrayBuilderAsRefAnalyzer>;

namespace Razor.Diagnostics.Analyzers.Test;

public class PooledArrayBuilderAsRefAnalyzerTests
{
    private const string PooledArrayBuilderSource = """
        namespace Microsoft.AspNetCore.Razor.PooledObjects
        {
            internal struct PooledArrayBuilder<T> : System.IDisposable
            {
                public void Dispose() { }
            }

            internal static class PooledArrayBuilderExtensions
            {
                public static ref PooledArrayBuilder<T> AsRef<T>(this in PooledArrayBuilder<T> array) => throw null;
            }
        }
        """;

    [Fact]
    public Task TestUsingVariable_CSharpAsync()
    {
        var code = $$"""
            using Microsoft.AspNetCore.Razor.PooledObjects;

            class C
            {
                void Method()
                {
                    using (var array = new PooledArrayBuilder<int>())
                    {
                        ref var arrayRef1 = ref array.AsRef();
                        ref var arrayRef2 = ref PooledArrayBuilderExtensions.AsRef(in array);
                    }
                }
            }

            {{PooledArrayBuilderSource}}
            """;

        return new VerifyCS.Test(code).RunAsync();
    }

    [Fact]
    public Task TestUsingDeclarationVariable_CSharpAsync()
    {
        var code = $$"""
            using Microsoft.AspNetCore.Razor.PooledObjects;

            class C
            {
                void Method()
                {
                    using var array = new PooledArrayBuilder<int>();
                    ref var arrayRef1 = ref array.AsRef();
                    ref var arrayRef2 = ref PooledArrayBuilderExtensions.AsRef(in array);
                }
            }

            {{PooledArrayBuilderSource}}
            """;

        return new VerifyCS.Test(code).RunAsync();
    }

    [Fact]
    public Task TestNonUsingVariable_CSharpAsync()
    {
        var code = $$"""
            using Microsoft.AspNetCore.Razor.PooledObjects;

            class C
            {
                void Method()
                {
                    var array = new PooledArrayBuilder<int>();
                    ref var arrayRef1 = ref [|array.AsRef()|];
                    ref var arrayRef2 = ref [|PooledArrayBuilderExtensions.AsRef(in array)|];
                }
            }

            {{PooledArrayBuilderSource}}
            """;

        return new VerifyCS.Test(code).RunAsync();
    }
}
