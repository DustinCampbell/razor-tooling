// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Razor.Diagnostics.Analyzers.Test.CSharpAnalyzerVerifier<Razor.Diagnostics.Analyzers.ReadOnlyListForEachAnalyzer>;

namespace Razor.Diagnostics.Analyzers.Test;

public class ReadOnlyListUsageAnalyzerTest
{
    [Fact]
    public Task TestForEachOfReadOnlyList()
    {
        var code = $$"""
            using System.Collections.Generic;

            class C
            {
                void Method(IReadOnlyList<int> list)
                {
                    foreach (var item in [|list|])
                    {
                    }
                }
            }
            """;

        return new VerifyCS.Test(code).RunAsync();
    }
}
