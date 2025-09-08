using Darp.Results.Analyzers.Rules;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verifier = Microsoft.CodeAnalysis.Testing.AnalyzerVerifier<
    Darp.Results.Analyzers.Rules.ResultReturnValueUsedAnalyzer,
    Darp.Results.Analyzers.Tests.ResultAnalyzerTest<Darp.Results.Analyzers.Rules.ResultReturnValueUsedAnalyzer>,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace Darp.Results.Analyzers.Tests.Rules;

public sealed class ResultReturnValueUsedAnalyzerTests
{
    [Fact]
    public async Task Discard_ShouldNotWarn()
    {
        const string text = """
            Result<int, string> Do() {
                return null!;
            }
            _ = Do();
            """;

        await ResultHelpers.VerifyInMethodAsync<ResultReturnValueUsedAnalyzer>(text);
    }

    [Fact]
    public async Task Variable_ShouldNotWarn()
    {
        const string text = """
            Result<int, string> Do() {
                return null!;
            }
            var myVar = Do();
            """;

        await ResultHelpers.VerifyInMethodAsync<ResultReturnValueUsedAnalyzer>(text);
    }

    [Fact]
    public async Task UserDefinedMethod_ShouldWarn()
    {
        const string text = """
            Result<int, string> Do() {
                return null!;
            }
            Do();
            """;

        DiagnosticResult expected = Verifier.Diagnostic().WithLocation(9, 1).WithArguments("Do");
        await ResultHelpers.VerifyInMethodAsync<ResultReturnValueUsedAnalyzer>(text, expected);
    }

    [Fact]
    public async Task ClassWithMyCompanyTitle_AlertDiagnostic()
    {
        const string text = """
            Result<int, string> myResult = 1;
            myResult.Map(x => "");
            """;

        DiagnosticResult expected = Verifier.Diagnostic().WithLocation(7, 1).WithArguments("Map");
        await ResultHelpers.VerifyInMethodAsync<ResultReturnValueUsedAnalyzer>(text, expected);
    }

    // With tasks

    [Fact]
    public async Task Task_NoAwait_ShouldNotWarn()
    {
        const string text = """
            Task<Result<int, string>> MethodWithTask() => Task.FromResult<Result<int, string>>(null!);
            ValueTask<Result<int, string>> MethodWithValueTask() => ValueTask.FromResult<Result<int, string>>(null!);
            MethodWithTask();
            MethodWithValueTask();
            """;

        await ResultHelpers.VerifyInAsyncMethodAsync<ResultReturnValueUsedAnalyzer>(text);
    }

    [Fact]
    public async Task Task_Discard_ShouldNotWarn()
    {
        const string text = """
            Task<Result<int, string>> MethodWithTask() => Task.FromResult<Result<int, string>>(null!);
            ValueTask<Result<int, string>> MethodWithValueTask() => ValueTask.FromResult<Result<int, string>>(null!);
            _ = await MethodWithTask();
            _ = await MethodWithValueTask();
            """;

        await ResultHelpers.VerifyInAsyncMethodAsync<ResultReturnValueUsedAnalyzer>(text);
    }

    [Fact]
    public async Task Task_Variable_ShouldNotWarn()
    {
        const string text = """
            Task<Result<int, string>> MethodWithTask() => Task.FromResult<Result<int, string>>(null!);
            ValueTask<Result<int, string>> MethodWithValueTask() => ValueTask.FromResult<Result<int, string>>(null!);
            var myVar = await MethodWithTask();
            var myVar2 = await MethodWithValueTask();
            """;

        await ResultHelpers.VerifyInAsyncMethodAsync<ResultReturnValueUsedAnalyzer>(text);
    }

    [Fact]
    public async Task Task_ShouldWarn()
    {
        const string text = """
            Task<Result<int, string>> MethodWithTask() => Task.FromResult<Result<int, string>>(null!);
            ValueTask<Result<int, string>> MethodWithValueTask() => ValueTask.FromResult<Result<int, string>>(null!);
            await MethodWithTask();
            await MethodWithValueTask();
            """;

        DiagnosticResult expected1 = Verifier.Diagnostic().WithLocation(8, 7).WithArguments("MethodWithTask");
        DiagnosticResult expected2 = Verifier.Diagnostic().WithLocation(9, 7).WithArguments("MethodWithValueTask");
        await ResultHelpers.VerifyInAsyncMethodAsync<ResultReturnValueUsedAnalyzer>(text, expected1, expected2);
    }
}
