using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Darp.Results.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HandleSwitchCorrectlyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_missingArmRule = new(
        RuleIdentifiers.SwitchExpressionMissingArmIdentifier,
        title: "Switch expression on Result should handle both ok and err case",
        messageFormat: "Switch expression on Result does not handle all cases. For example, '{0}' is not covered.",
        RuleCategories.Usage,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Switch expressions on Result types should handle both Ok and Err cases, or use a catch-all pattern to ensure all possible states are considered.",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.SwitchExpressionMissingArmIdentifier)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_missingArmRule];

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(Analyze, OperationKind.SwitchExpression);
    }

    private static void Analyze(OperationAnalysisContext context)
    {
        var expression = (ISwitchExpressionOperation)context.Operation;

        // Only continue running of the type is an analyzer
        ITypeSymbol? resultType = expression.Value.Type;
        if (!resultType.IsOrExtendsResult())
            return;

        // Nothing to do if all cases are covered
        if (expression.IsExhaustive)
            return;

        bool hasOk = false;
        bool hasErr = false;
        foreach (ISwitchExpressionArmOperation arm in expression.Arms)
        {
            if (arm.Pattern.NarrowedType.IsOkResult(resultType))
                hasOk = true;
            if (arm.Pattern.NarrowedType.IsErrorResult(resultType))
                hasErr = true;
        }

        // Ok, if both ok and err exist
        if (hasOk && hasErr)
            return;
        var syntax = (SwitchExpressionSyntax)expression.Syntax;
        var missingArms = new Dictionary<string, string?>();
        if (!hasOk)
            missingArms.Add("OkName", Helpers.GetResultCaseName(resultType, "Ok"));
        if (!hasErr)
            missingArms.Add("ErrName", Helpers.GetResultCaseName(resultType, "Err"));
        var diagnostic = Diagnostic.Create(
            s_missingArmRule,
            syntax.SwitchKeyword.GetLocation(),
            missingArms.ToImmutableDictionary(),
            missingArms.FirstOrDefault().Value
        );
        context.ReportDiagnostic(diagnostic);
    }
}
