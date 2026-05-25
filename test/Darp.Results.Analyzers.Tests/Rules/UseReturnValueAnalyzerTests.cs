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
    public void HelpLink_ShouldBeCorrect()
    {
        ResultHelpers.VerifyHelpLink<ResultReturnValueUsedAnalyzer>("DR0001");
    }

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
            MethodWithTask().ConfigureAwait(false);
            MethodWithValueTask().ConfigureAwait(false);
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
            _ = await MethodWithTask().ConfigureAwait(false);
            _ = await MethodWithValueTask().ConfigureAwait(false);
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
            var myVar3 = await MethodWithTask().ConfigureAwait(false);
            var myVar4 = await MethodWithValueTask().ConfigureAwait(false);
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
            await MethodWithTask().ConfigureAwait(false);
            await MethodWithValueTask().ConfigureAwait(false);
            """;

        DiagnosticResult expected1 = Verifier.Diagnostic().WithLocation(8, 7).WithArguments("MethodWithTask");
        DiagnosticResult expected2 = Verifier.Diagnostic().WithLocation(9, 7).WithArguments("MethodWithValueTask");
        DiagnosticResult expected3 = Verifier.Diagnostic().WithLocation(10, 7).WithArguments("ConfigureAwait");
        DiagnosticResult expected4 = Verifier.Diagnostic().WithLocation(11, 7).WithArguments("ConfigureAwait");
        await ResultHelpers.VerifyInAsyncMethodAsync<ResultReturnValueUsedAnalyzer>(
            text,
            expected1,
            expected2,
            expected3,
            expected4
        );
    }

    [Fact]
    public async Task CustomAwaitable_ShouldWarn()
    {
        const string text = """
            public static async Task MyAsyncMethod()
            {
                await GetMyCustomAwaitable();
                var result = await GetMyCustomAwaitable();
            }

            private static MyAwaitable GetMyCustomAwaitable() => new();

            private readonly struct MyAwaitable
            {
                public MyAwaiter GetAwaiter() => new();
            }

            private readonly struct MyAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public Result<int, string> GetResult() => 1;
                public void OnCompleted(Action continuation) { }
            }
            """;

        DiagnosticResult expected = Verifier.Diagnostic().WithLocation(10, 11).WithArguments("GetMyCustomAwaitable");
        await ResultHelpers.VerifyInAsyncClassAsync<ResultReturnValueUsedAnalyzer>(text, expected);
    }
}
