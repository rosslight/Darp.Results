using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Darp.Results.Analyzers.Rules;

/// <summary> Warns when a project consumes Darp.Results with a C# version that does not understand unions. </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LanguageVersionTooLowAnalyzer : DiagnosticAnalyzer
{
    // LanguageVersion.CSharp15. The referenced Roslyn may predate the enum member.
    private const LanguageVersion RequiredLanguageVersion = (LanguageVersion)1500;

    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.LanguageVersionTooLowIdentifier,
        title: "Darp.Results requires C# 15",
        messageFormat: "Darp.Results requires C# 15 or later, but this project uses C# {0}. Use the .NET 11 SDK and set <LangVersion>15</LangVersion> or newer, otherwise Ok/Err conversions and switch patterns do not compile.",
        RuleCategories.Compatibility,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Result<TValue, TError> is a C# 15 union. Older compilers treat it as a plain struct, so case conversions and exhaustive switches over Ok and Err fail.",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.LanguageVersionTooLowIdentifier),
        customTags: WellKnownDiagnosticTags.CompilationEnd
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        if (context.Compilation is not CSharpCompilation compilation)
            return;
        if (compilation.LanguageVersion >= RequiredLanguageVersion)
            return;
        INamedTypeSymbol? resultType = compilation.GetTypeByMetadataName("Darp.Results.Result`2");
        if (resultType is null || SymbolEqualityComparer.Default.Equals(resultType.ContainingAssembly, compilation.Assembly))
            return;

        context.ReportDiagnostic(
            Diagnostic.Create(s_rule, Location.None, compilation.LanguageVersion.ToDisplayString())
        );
    }
}
