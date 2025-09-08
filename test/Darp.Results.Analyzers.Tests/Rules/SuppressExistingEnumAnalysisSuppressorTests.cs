using Darp.Results.Analyzers.Rules;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Darp.Results.Analyzers.Tests.Rules;

public sealed class SuppressExistingEnumAnalysisSuppressorTests
{
    [Fact]
    public async Task NonResultSwitch_ShouldWarn()
    {
        const string text = """
            Result<int, string> Do(int r) {
                return r switch {
                    1 => 1,
                    2 => 2,
                };
            }
            """;

        DiagnosticResult expected = DiagnosticResult
            .CompilerWarning("CS8509")
            .WithSpan(7, 14, 7, 20)
            .WithArguments("0");
        await ResultHelpers.VerifyInMethodAsync<SuppressExistingEnumAnalysisSuppressor>(text, ["CS8509"], expected);
    }

    [Fact(Skip = "Somehow runtime issues")]
    public async Task ResultSwitch_ShouldNotWarn()
    {
        const string text = """
            Result<int, string> Do(Result<int, string> r) {
                return r switch {
                    Result.Ok<int, string> ok => ok.Value,
                    Result.Err<int, string> err => err.Error,
                };
            }
            """;

        await ResultHelpers.VerifyInMethodAsync<SuppressExistingEnumAnalysisSuppressor>(text, ["CS8509"]);
    }
}
