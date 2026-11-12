using Darp.Results.Analyzers.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Darp.Results.Analyzers.Tests.Rules;

public sealed class LanguageVersionTooLowAnalyzerTests
{
    private const string Source = """
        using Darp.Results;

        static class TestClass
        {
            static Result<int, string> Run() => 1;
        }
        """;

    [Fact]
    public void HelpLink_ShouldBeCorrect()
    {
        ResultHelpers.VerifyHelpLink<LanguageVersionTooLowAnalyzer>("DR0006");
    }

    [Fact]
    public async Task UnionLanguageVersion_ShouldNotWarn()
    {
        // Stands in for C# 15 until a published Roslyn package knows it
        await RunAsync(Source, LanguageVersion.Preview);
    }

    [Fact]
    public async Task CSharp14_ShouldWarn()
    {
        DiagnosticResult expected = new DiagnosticResult("DR0006", DiagnosticSeverity.Warning).WithArguments("14.0");
        await RunAsync(Source, LanguageVersion.CSharp14, expected);
    }

    [Fact]
    public async Task CSharp14_WithoutDarpResults_ShouldNotWarn()
    {
        var test = CreateTest("static class TestClass { }", LanguageVersion.CSharp14);
        test.TestState.AdditionalReferences.Clear();
        await test.RunAsync(CancellationToken.None);
    }

    private static Task RunAsync(string source, LanguageVersion version, params DiagnosticResult[] expected)
    {
        var test = CreateTest(source, version);
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync(CancellationToken.None);
    }

    private static ResultAnalyzerTest<LanguageVersionTooLowAnalyzer> CreateTest(string source, LanguageVersion version)
    {
        var test = new ResultAnalyzerTest<LanguageVersionTooLowAnalyzer> { TestCode = source };
        test.SolutionTransforms.Add(
            (solution, projectId) =>
            {
                var options = (CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!;
                return solution.WithProjectParseOptions(projectId, options.WithLanguageVersion(version));
            }
        );
        return test;
    }
}
