using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Darp.Results.Analyzers.Tests;

public static class ResultHelpers
{
    private static string InMethod(string body)
    {
        return $$"""
            using Darp.Results;

            public static class MyTestClass
            {
                public static void MyMethod() {
            {{body}}
                }
            }
            """;
    }

    public static Task VerifyInMethodAsync<TAnalyzer>(string methodBody, params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        return AnalyzerVerifier<TAnalyzer, ResultAnalyzerTest<TAnalyzer>, DefaultVerifier>.VerifyAnalyzerAsync(
            InMethod(methodBody),
            expected
        );
    }
}
