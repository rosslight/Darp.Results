using System.Collections.Immutable;
using System.Composition;
using Darp.Results.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace Darp.Results.CodeFixers.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class DocumentEscapingExceptionsCodeFixer : CodeFixProvider
{
    private const string EquivalenceKey = "DocumentEscapingExceptions";

    public override ImmutableArray<string> FixableDiagnosticIds => [RuleIdentifiers.KnownExceptionMayEscapeIdentifier];

    public override FixAllProvider? GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        MemberDeclarationSyntax? declaration = root is null
            ? null
            : FindDocumentableDeclaration(root.FindNode(context.Span, getInnermostNodeForTie: true));
        if (declaration is null)
            return;

        SemanticModel? semanticModel = await context
            .Document.GetSemanticModelAsync(context.CancellationToken)
            .ConfigureAwait(false);
        if (semanticModel is null)
            return;

        ImmutableArray<ExceptionDocumentation> exceptions = GetExceptions(
            context.Diagnostics,
            semanticModel.Compilation
        );
        if (exceptions.IsDefaultOrEmpty)
            return;

        string memberName = declaration switch
        {
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            IndexerDeclarationSyntax => "this[]",
            _ => "member",
        };
        string title = $"Document escaping exceptions on '{memberName}'";
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                cancellationToken =>
                    AddExceptionDocumentation(context.Document, declaration, exceptions, cancellationToken),
                equivalenceKey: EquivalenceKey
            ),
            context.Diagnostics
        );
    }

    private static MemberDeclarationSyntax? FindDocumentableDeclaration(SyntaxNode node)
    {
        foreach (SyntaxNode candidate in node.AncestorsAndSelf())
        {
            switch (candidate)
            {
                case AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax:
                    return null;
                case MethodDeclarationSyntax method:
                    return method;
                case PropertyDeclarationSyntax property:
                    return property;
                case IndexerDeclarationSyntax indexer:
                    return indexer;
            }
        }
        return null;
    }

    private static ImmutableArray<ExceptionDocumentation> GetExceptions(
        ImmutableArray<Diagnostic> diagnostics,
        Compilation compilation
    )
    {
        var exceptions = ImmutableArray.CreateBuilder<ExceptionDocumentation>();
        var seenDocumentationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (Diagnostic diagnostic in diagnostics)
        {
            if (!diagnostic.Properties.TryGetValue("ExceptionTypes", out string? value) || value is null)
                continue;

            foreach (string documentationId in value.Split(';'))
            {
                if (
                    !seenDocumentationIds.Add(documentationId)
                    || DocumentationCommentId.GetFirstSymbolForDeclarationId(documentationId, compilation)
                        is not INamedTypeSymbol exceptionType
                )
                {
                    continue;
                }

                exceptions.Add(
                    new ExceptionDocumentation(
                        documentationId,
                        exceptionType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                    )
                );
            }
        }

        return exceptions.OrderBy(exception => exception.Cref, StringComparer.Ordinal).ToImmutableArray();
    }

    private static async Task<Document> AddExceptionDocumentation(
        Document document,
        MemberDeclarationSyntax declaration,
        ImmutableArray<ExceptionDocumentation> exceptions,
        CancellationToken cancellationToken
    )
    {
        SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return document;

        HashSet<string> existingDocumentationIds = GetExistingExceptionDocumentationIds(
            declaration,
            semanticModel,
            cancellationToken
        );
        ExceptionDocumentation[] exceptionsToAdd = exceptions
            .Where(exception => !existingDocumentationIds.Contains(exception.DocumentationId))
            .ToArray();
        if (exceptionsToAdd.Length == 0)
            return document;

        SourceText text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        string newLine = GetNewLine(text);
        TextLine declarationLine = text.Lines.GetLineFromPosition(declaration.SpanStart);
        string linePrefix = text.ToString(TextSpan.FromBounds(declarationLine.Start, declaration.SpanStart));
        bool startsOnOwnLine = linePrefix.All(char.IsWhiteSpace);
        string indentation = startsOnOwnLine ? linePrefix : string.Empty;
        string documentation =
            (startsOnOwnLine ? string.Empty : newLine)
            + string.Join(
                newLine + indentation,
                exceptionsToAdd.Select(exception =>
                    $"/// <exception cref=\"{EscapeXmlAttribute(exception.Cref)}\"></exception>"
                )
            )
            + newLine
            + indentation;

        SyntaxTriviaList updatedLeadingTrivia = declaration
            .GetLeadingTrivia()
            .AddRange(SyntaxFactory.ParseLeadingTrivia(documentation));
        MemberDeclarationSyntax updatedDeclaration = declaration
            .WithLeadingTrivia(updatedLeadingTrivia)
            .WithAdditionalAnnotations(Formatter.Annotation);
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        return root is null ? document : document.WithSyntaxRoot(root.ReplaceNode(declaration, updatedDeclaration));
    }

    private static HashSet<string> GetExistingExceptionDocumentationIds(
        MemberDeclarationSyntax declaration,
        SemanticModel semanticModel,
        CancellationToken cancellationToken
    )
    {
        var documentationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (
            XmlCrefAttributeSyntax attribute in declaration
                .GetLeadingTrivia()
                .Select(trivia => trivia.GetStructure())
                .OfType<DocumentationCommentTriviaSyntax>()
                .SelectMany(documentation => documentation.DescendantNodes().OfType<XmlCrefAttributeSyntax>())
        )
        {
            string? elementName = attribute.Parent switch
            {
                XmlElementStartTagSyntax startTag => startTag.Name.LocalName.ValueText,
                XmlEmptyElementSyntax emptyElement => emptyElement.Name.LocalName.ValueText,
                _ => null,
            };
            if (elementName != "exception")
                continue;

            ISymbol? symbol = semanticModel.GetSymbolInfo(attribute.Cref, cancellationToken).Symbol;
            if (symbol?.GetDocumentationCommentId() is { } documentationId)
                documentationIds.Add(documentationId);
        }
        return documentationIds;
    }

    private static string GetNewLine(SourceText text)
    {
        foreach (TextLine line in text.Lines)
        {
            if (line.EndIncludingLineBreak > line.End)
                return text.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak));
        }
        return "\n";
    }

    private static string EscapeXmlAttribute(string value)
    {
        return value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private readonly struct ExceptionDocumentation(string documentationId, string cref)
    {
        public string DocumentationId { get; } = documentationId;
        public string Cref { get; } = cref;
    }
}
