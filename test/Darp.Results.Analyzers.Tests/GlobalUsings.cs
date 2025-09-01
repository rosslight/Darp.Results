global using Verifier = Microsoft.CodeAnalysis.Testing.AnalyzerVerifier<
    Darp.Results.Analyzers.Rules.AbstractTypesShouldNotHaveConstructorsAnalyzer,
    Darp.Results.Analyzers.Tests.ResultAnalyzerTest,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;
