using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Darp.Results.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ResultReturnValueUsedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.UseReturnValueIdentifier,
        title: "The return value of a result should be checked",
        messageFormat: "Because the result of {0} is not checked, a failed result might go unnoticed. Check the return value.",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseReturnValueIdentifier)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
    }

    private static void AnalyzeOperation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        ITypeSymbol? returnType = invocation.Type;
        if (!returnType.IsOrExtendsResult(out bool isTask))
            return;
        if (!invocation.IsUnused(isTask, countDiscardsAsUnused: false))
            return;
        // Build a helpful diagnostic.
        string methodName = invocation.TargetMethod.Name;

        var diagnostic = Diagnostic.Create(s_rule, invocation.Syntax.GetLocation(), methodName);
        context.ReportDiagnostic(diagnostic);
    }
}
