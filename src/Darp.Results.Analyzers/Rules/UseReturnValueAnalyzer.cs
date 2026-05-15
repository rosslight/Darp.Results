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

        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeAwait, OperationKind.Await);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        ITypeSymbol? returnType = invocation.Type;
        if (!returnType.IsResult())
            return;
        if (!IsExpressionStatementAfterConversions(invocation))
            return;
        ReportDiagnostic(context, invocation, invocation.TargetMethod.Name);
    }

    private static void AnalyzeAwait(OperationAnalysisContext context)
    {
        var awaitOperation = (IAwaitOperation)context.Operation;
        if (!awaitOperation.Type.IsResult())
            return;
        if (!IsExpressionStatementAfterConversions(awaitOperation))
            return;

        IOperation awaitedOperation = StripImplicitConversions(awaitOperation.Operation);
        string expressionName = GetAwaitedExpressionName(awaitedOperation);
        ReportDiagnostic(context, awaitedOperation, expressionName);
    }

    private static bool IsExpressionStatementAfterConversions(IOperation operation)
    {
        IOperation node = operation;
        while (node.Parent is IConversionOperation { IsImplicit: true } conversion)
            node = conversion;

        return node.Parent is IExpressionStatementOperation;
    }

    private static IOperation StripImplicitConversions(IOperation operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } conversion)
            operation = conversion.Operand;

        return operation;
    }

    private static string GetAwaitedExpressionName(IOperation operation)
    {
        return operation switch
        {
            IInvocationOperation invocation => invocation.TargetMethod.Name,
            IPropertyReferenceOperation property => property.Property.Name,
            IObjectCreationOperation creation => creation.Constructor?.ContainingType.Name ?? "await expression",
            ILocalReferenceOperation local => local.Local.Name,
            _ => "await expression",
        };
    }

    private static void ReportDiagnostic(OperationAnalysisContext context, IOperation operation, string expressionName)
    {
        var diagnostic = Diagnostic.Create(s_rule, operation.Syntax.GetLocation(), expressionName);
        context.ReportDiagnostic(diagnostic);
    }
}
