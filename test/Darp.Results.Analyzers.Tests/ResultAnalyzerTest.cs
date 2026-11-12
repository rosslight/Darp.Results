using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Darp.Results.Analyzers.Tests;

public sealed class ResultAnalyzerTest<TAnalyzer> : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public ResultAnalyzerTest()
    {
        TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(Result<,>).Assembly.Location));
        ReferenceAssemblies = ReferenceAssemblies.Net.Net110;
        SolutionTransforms.Add(UseUnionLanguageVersion);
    }

    /// <summary> No published Roslyn package knows C# 15 yet. Its preview includes unions. </summary>
    internal static Solution UseUnionLanguageVersion(Solution solution, ProjectId projectId)
    {
        var options = (CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!;
        return solution.WithProjectParseOptions(projectId, options.WithLanguageVersion(LanguageVersion.Preview));
    }
}
