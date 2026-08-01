using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Darp.Results.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DocumentedExceptionMayEscapeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.DocumentedExceptionMayEscapeIdentifier,
        title: "Documented exception may escape a Result-returning function",
        messageFormat: "The following XML-documented exception types may escape this Result-returning function: {0}. Catch and return them as errors, document them with exception elements, or exclude the invoked member.",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "XML-documented exceptions should be returned through the Result error channel, explicitly allowed, declared in the function's XML documentation, or excluded for members whose documented conditions cannot apply.",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DocumentedExceptionMayEscapeIdentifier)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var state = new ExceptionEscapeAnalysis.AnalyzerState(
                compilationContext.Compilation,
                compilationContext.Options.AdditionalFiles
            );
            compilationContext.RegisterOperationAction(
                operationContext => ExceptionEscapeAnalysis.AnalyzeDocumentedMember(operationContext, state, s_rule),
                OperationKind.Invocation,
                OperationKind.ObjectCreation,
                OperationKind.PropertyReference,
                OperationKind.Binary,
                OperationKind.Unary,
                OperationKind.Conversion,
                OperationKind.CompoundAssignment,
                OperationKind.Increment,
                OperationKind.Decrement
            );
        });
    }
}
