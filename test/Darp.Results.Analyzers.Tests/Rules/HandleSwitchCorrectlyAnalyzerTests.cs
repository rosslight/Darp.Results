using Darp.Results.Analyzers.Rules;
using Darp.Results.CodeFixers.Rules;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verifier = Microsoft.CodeAnalysis.Testing.AnalyzerVerifier<
    Darp.Results.Analyzers.Rules.HandleSwitchCorrectlyAnalyzer,
    Darp.Results.Analyzers.Tests.ResultAnalyzerTest<Darp.Results.Analyzers.Rules.HandleSwitchCorrectlyAnalyzer>,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace Darp.Results.Analyzers.Tests.Rules;

public sealed class HandleSwitchCorrectlyAnalyzerTests
{
    [Fact]
    public async Task CompleteSwitch_ShouldNotError()
    {
        const string text = """
            Result<int, string> Do(Result<int, string> r) {
                return r switch {
                    Result.Ok<int, string> ok => ok.Value,
                    Result.Err<int, string> err => err.Error,
                };
            }
            """;

        await ResultHelpers.VerifyInMethodAsync<HandleSwitchCorrectlyAnalyzer>(text);
    }

    [Fact]
    public async Task DiscardErr_ShouldNotError()
    {
        const string text = """
            Result<int, string> Do(Result<int, string> r) {
                return r switch {
                    Result.Ok<int, string> ok => ok.Value,
                    _ => "error",
                };
            }
            """;

        await ResultHelpers.VerifyInMethodAsync<HandleSwitchCorrectlyAnalyzer>(text);
    }

    [Fact]
    public async Task DiscardOk_ShouldNotError()
    {
        const string text = """
            Result<int, string> Do(Result<int, string> r) {
                return r switch {
                    Result.Err<int, string> err => err.Error,
                    _ => 1,
                };
            }
            """;

        await ResultHelpers.VerifyInMethodAsync<HandleSwitchCorrectlyAnalyzer>(text);
    }

    [Fact]
    public async Task MissingErrorCase_ShouldError()
    {
        const string text = """
            Result<int, string> Do(Result<int, string> r) {
                return r switch {
                    Result.Ok<int, string> ok => ok.Value,
                };
            }
            """;
        const string fixedText = """
            Result<int, string> Do(Result<int, string> r) {
                return r switch {
                    Result.Ok<int, string> ok => ok.Value,
                    Result.Err<int, string> => throw new System.NotImplementedException()
                };
            }
            """;

        DiagnosticResult expected = Verifier
            .Diagnostic()
            .WithSpan(7, 14, 7, 20)
            .WithArguments("Result.Err<int, string>");
        await ResultHelpers.VerifyCodeFixAsync<HandleSwitchCorrectlyAnalyzer, HandleSwitchCorrectlyCodeFixer>(
            text,
            expected,
            fixedText
        );
    }

    [Fact]
    public async Task MissingOkCase_ShouldError()
    {
        const string text = """
            Result<int, string> Do(Result<int, string> r) {
                return r switch {
                    Result.Err<int, string> err => err.Error,
                };
            }
            """;
        const string fixedText = """
            Result<int, string> Do(Result<int, string> r) {
                return r switch {
                    Result.Ok<int, string> => throw new System.NotImplementedException(),
                    Result.Err<int, string> err => err.Error
                };
            }
            """;

        DiagnosticResult expected = Verifier
            .Diagnostic()
            .WithSpan(7, 14, 7, 20)
            .WithArguments("Result.Ok<int, string>");
        await ResultHelpers.VerifyCodeFixAsync<HandleSwitchCorrectlyAnalyzer, HandleSwitchCorrectlyCodeFixer>(
            text,
            expected,
            fixedText
        );
    }

    [Fact]
    public async Task MissingAllCases_ShouldError()
    {
        const string text = """
            Result<int, string> Do(Result<int, string> r) {
                return r switch {
                };
            }
            """;
        const string fixedText = """
            Result<int, string> Do(Result<int, string> r) {
                return r switch {
                    Result.Ok<int, string> => throw new System.NotImplementedException(),
                    Result.Err<int, string> => throw new System.NotImplementedException()
                };
            }
            """;

        DiagnosticResult expected = Verifier
            .Diagnostic()
            .WithSpan(7, 14, 7, 20)
            .WithArguments("Result.Ok<int, string>");
        await ResultHelpers.VerifyCodeFixAsync<HandleSwitchCorrectlyAnalyzer, HandleSwitchCorrectlyCodeFixer>(
            text,
            expected,
            fixedText
        );
    }
}
