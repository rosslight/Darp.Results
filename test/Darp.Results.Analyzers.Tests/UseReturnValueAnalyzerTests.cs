using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Darp.Results.Analyzers.Tests;

public class SampleSyntaxAnalyzerTests
{
    [Fact]
    public async Task ClassWithMyCompanyTitle_AlertDiagnostic()
    {
        const string text = """
            Result<int, string> Do() {
                return null!;
            }
            Do();
            """;

        DiagnosticResult expected = Verifier
            .Diagnostic()
            .WithLocation(9, 1)
            .WithArguments("Do()", "Result<int, string>");
        await Verifier.VerifyAnalyzerAsync(ResultHelpers.InMethod(text), expected);
    }
}
