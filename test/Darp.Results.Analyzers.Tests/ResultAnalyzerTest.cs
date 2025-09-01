using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Darp.Results.Analyzers.Tests;

public sealed class ResultAnalyzerTest
    : CSharpAnalyzerTest<Rules.AbstractTypesShouldNotHaveConstructorsAnalyzer, DefaultVerifier>
{
    public ResultAnalyzerTest()
    {
        TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(Result<,>).Assembly.Location));
        ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
    }
}
