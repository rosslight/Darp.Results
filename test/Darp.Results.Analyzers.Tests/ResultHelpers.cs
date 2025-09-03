using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Darp.Results.Analyzers.Tests;

public static class ResultHelpers
{
    private static string InMethod(string body)
    {
        return $$"""
            using Darp.Results;

            internal static class MyTestClass
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

    public static Task VerifyInMethodAsync<TAnalyzer>(
        string methodBody,
        string[] allowedCompilerDiagnostics,
        params DiagnosticResult[] expected
    )
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var test = new ResultAnalyzerTest<TAnalyzer> { TestCode = InMethod(methodBody) };
        test.ExpectedDiagnostics.AddRange(expected);
        test.CompilerDiagnostics = CompilerDiagnostics.Warnings; // Suppress everything globally, then re-enable CS8509 as a warning
        test.SolutionTransforms.Add(
            (solution, projectId) =>
            {
                Project project = solution.GetProject(projectId)!;
                var opts = (CSharpCompilationOptions)project.CompilationOptions!;

                opts = opts.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
                    .WithSpecificDiagnosticOptions(
                        opts.SpecificDiagnosticOptions.SetItems(
                            allowedCompilerDiagnostics.Select(x => new KeyValuePair<string, ReportDiagnostic>(
                                x,
                                ReportDiagnostic.Warn
                            ))
                        )
                    );

                return solution.WithProjectCompilationOptions(projectId, opts);
            }
        );
        return test.RunAsync(CancellationToken.None);
    }

    public static Task VerifyCodeFixAsync<TAnalyzer, TCodeFix>(
        string source,
        DiagnosticResult expected,
        string fixedSource
    )
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new() =>
        VerifyCodeFixAsync<TAnalyzer, TCodeFix>(source, [expected], fixedSource);

    public static Task VerifyCodeFixAsync<TAnalyzer, TCodeFix>(
        string source,
        DiagnosticResult[] expected,
        string fixedSource
    )
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            TestCode = InMethod(source),
            FixedCode = InMethod(fixedSource),
        };
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(Result<,>).Assembly.Location));
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync(CancellationToken.None);
    }
}
