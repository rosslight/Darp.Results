using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Darp.Results.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SuppressExistingEnumAnalysisSuppressor : DiagnosticSuppressor
{
    private const string SuppressedDiagnostic = "CS8509";

    // The ID for *your* suppressor (pick something unique to your analyzer)
    private static readonly SuppressionDescriptor s_descriptor = new(
        id: RuleIdentifiers.SuppressSwitchExpressionAnalysisIdentifier,
        suppressedDiagnosticId: SuppressedDiagnostic, // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/pattern-matching-warnings
        justification: $"Result analysis is covered by {RuleIdentifiers.SwitchExpressionMissingArmIdentifier}."
    );

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => [s_descriptor];

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
        {
            if (diagnostic.Id != SuppressedDiagnostic)
                continue;

            SyntaxTree? tree = diagnostic.Location.SourceTree;
            if (tree is null)
                continue;

            SemanticModel model = context.GetSemanticModel(tree);
            SyntaxNode node = tree.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan);
            if (node is not SwitchExpressionSyntax expression)
                continue;
            ITypeSymbol? typeInfo = model.GetTypeInfo(expression.GoverningExpression, context.CancellationToken).Type;
            if (!typeInfo.IsOrExtendsResult())
                continue;
            context.ReportSuppression(Suppression.Create(s_descriptor, diagnostic));
        }
    }
}
