using Darp.Results.Analyzers.Rules;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verifier = Microsoft.CodeAnalysis.Testing.AnalyzerVerifier<
    Darp.Results.Analyzers.Rules.AbstractTypesShouldNotHaveConstructorsAnalyzer,
    Darp.Results.Analyzers.Tests.ResultAnalyzerTest<Darp.Results.Analyzers.Rules.AbstractTypesShouldNotHaveConstructorsAnalyzer>,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace Darp.Results.Analyzers.Tests.Rules;

public class SampleSyntaxAnalyzerTests
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

        await ResultHelpers.VerifyInMethodAsync<AbstractTypesShouldNotHaveConstructorsAnalyzer>(text);
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

        await ResultHelpers.VerifyInMethodAsync<AbstractTypesShouldNotHaveConstructorsAnalyzer>(text);
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
        await ResultHelpers.VerifyInMethodAsync<AbstractTypesShouldNotHaveConstructorsAnalyzer>(text, expected);
    }

    [Fact]
    public async Task ClassWithMyCompanyTitle_AlertDiagnostic()
    {
        const string text = """
            Result<int, string> myResult = 1;
            myResult.Map(x => "");
            """;

        DiagnosticResult expected = Verifier.Diagnostic().WithLocation(7, 1).WithArguments("Map");
        await ResultHelpers.VerifyInMethodAsync<AbstractTypesShouldNotHaveConstructorsAnalyzer>(text, expected);
    }
}
