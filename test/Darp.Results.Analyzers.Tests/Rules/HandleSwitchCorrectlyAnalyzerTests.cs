using Darp.Results.Analyzers.Rules;
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
    Result<int, string> Do(Result<int, string> r)
    {
        return r switch
        {
            Result<int, string>.Ok ok => ok.Value,
            Result<int, string>.Err err => err.Error,
        };
    }

    [Fact]
    public async Task CompleteSwitch_ShouldNotError()
    {
        const string text = """
            Result<int, string> Do(Result<int, string> r) {
                return r switch {
                    Result<int, string>.Ok ok => ok.Value,
                    Result<int, string>.Err err => err.Error,
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
                    Result<int, string>.Ok ok => ok.Value,
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
                    Result<int, string>.Err err => err.Error,
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
                    Result<int, string>.Ok ok => ok.Value,
                };
            }
            """;

        DiagnosticResult expected = Verifier
            .Diagnostic()
            .WithSpan(7, 14, 7, 20)
            .WithArguments("Result<int, string>.Err");
        await ResultHelpers.VerifyInMethodAsync<HandleSwitchCorrectlyAnalyzer>(text, expected);
    }

    [Fact]
    public async Task MissingOkCase_ShouldError()
    {
        const string text = """
            Result<int, string> Do(Result<int, string> r) {
                return r switch {
                    Result<int, string>.Err err => err.Error,
                };
            }
            """;

        DiagnosticResult expected = Verifier
            .Diagnostic()
            .WithSpan(7, 14, 7, 20)
            .WithArguments("Result<int, string>.Ok");
        await ResultHelpers.VerifyInMethodAsync<HandleSwitchCorrectlyAnalyzer>(text, expected);
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

        DiagnosticResult expected = Verifier
            .Diagnostic()
            .WithSpan(7, 14, 7, 20)
            .WithArguments("Result<int, string>.Ok");
        await ResultHelpers.VerifyInMethodAsync<HandleSwitchCorrectlyAnalyzer>(text, expected);
    }
}
