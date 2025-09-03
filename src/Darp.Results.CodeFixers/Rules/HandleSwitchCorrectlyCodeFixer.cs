using System.Collections.Immutable;
using System.Composition;
using Darp.Results.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Darp.Results.CodeFixers.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class HandleSwitchCorrectlyCodeFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [RuleIdentifiers.SwitchExpressionMissingArmIdentifier];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root?.FindNode(context.Span, getInnermostNodeForTie: true) is not SwitchExpressionSyntax nodeToFix)
            return;
        Diagnostic? diagnostic = context.Diagnostics.FirstOrDefault();
        if (diagnostic is null)
            return;
        List<string> propertiesToFix = [];
        if (diagnostic.Properties.TryGetValue("OkName", out string? okName) && okName is not null)
            propertiesToFix.Add(okName);
        if (diagnostic.Properties.TryGetValue("ErrName", out string? errName) && errName is not null)
            propertiesToFix.Add(errName);
        var title = "Add missing switch arm";
        var codeAction = CodeAction.Create(
            title,
            ct => AddMissingSwitchArm(context.Document, nodeToFix, propertiesToFix, ct),
            equivalenceKey: title
        );

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> AddMissingSwitchArm(
        Document document,
        SwitchExpressionSyntax nodeToFix,
        List<string> propertiesToFix,
        CancellationToken cancellationToken
    )
    {
        // Track existing arm patterns to avoid duplicates (simple, robust compare).
        var existing = new HashSet<string>(nodeToFix.Arms.Select(a => a.Pattern.ToString()), StringComparer.Ordinal);

        var toAdd = propertiesToFix.Where(p => !existing.Contains(p)).ToList();
        if (toAdd.Count == 0)
            return document;

        DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        // throw new NotImplementedException()
        ThrowExpressionSyntax notImplementedExpression = ThrowExpression(
            ObjectCreationExpression(QualifiedName(IdentifierName("System"), IdentifierName("NotImplementedException")))
                .WithArgumentList(ArgumentList())
        );
        // Build new arms: <identifier> => throw new ...
        IEnumerable<SwitchExpressionArmSyntax> newArms = toAdd.Select(p =>
            SwitchExpressionArm(
                    ConstantPattern(ParseExpression(p)),
                    whenClause: null,
                    expression: notImplementedExpression
                )
                .WithAdditionalAnnotations(Formatter.Annotation)
        );

        SeparatedSyntaxList<SwitchExpressionArmSyntax> currentArms = nodeToFix.Arms;
        int insertIndex = currentArms.IndexOf(x => x.Pattern is DiscardPatternSyntax);
        if (insertIndex < 0)
            insertIndex = currentArms.Count - 1;

        SwitchExpressionSyntax updatedSwitch = nodeToFix.WithArms(
            SeparatedList(currentArms.InsertRange(insertIndex + 1, newArms))
        );

        editor.ReplaceNode(nodeToFix, updatedSwitch);
        return editor.GetChangedDocument();
    }
}
