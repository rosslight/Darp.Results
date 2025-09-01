using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Darp.Results.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AbstractTypesShouldNotHaveConstructorsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.UseReturnValueIdentifier,
        title: "Abstract types should not have public or internal constructors",
        messageFormat: "Abstract types should not have public or internal constructors",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseReturnValueIdentifier)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
    }

    private static void AnalyzeOperation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        ITypeSymbol? returnType = invocation.Type;
        if (!returnType.IsOrExtendsResult())
            return;
        if (!invocation.IsUnused())
            return;

        // Build a helpful diagnostic.
        string methodName = invocation.TargetMethod.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
        string returnTypeDisplay = returnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        var diagnostic = Diagnostic.Create(s_rule, invocation.Syntax.GetLocation(), methodName, returnTypeDisplay);
        context.ReportDiagnostic(diagnostic);
    }
}
