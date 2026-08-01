using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Darp.Results.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class KnownExceptionMayEscapeAnalyzer : DiagnosticAnalyzer
{
    private const string AllowedExceptionTypesOption =
        "dotnet_diagnostic." + RuleIdentifiers.KnownExceptionMayEscapeIdentifier + ".allowed_exception_types";

    private static readonly ImmutableArray<string> s_defaultAllowedExceptionTypeNames =
    [
        "System.ArgumentException",
        "System.OperationCanceledException",
        "System.NotImplementedException",
        "System.NotSupportedException",
        "System.Diagnostics.UnreachableException",
        "System.ObjectDisposedException",
        "System.Runtime.CompilerServices.SwitchExpressionException",
    ];

    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.KnownExceptionMayEscapeIdentifier,
        title: "Known exception may escape a Result-returning function",
        messageFormat: "The following exception types may escape this Result-returning function: {0}. Catch and return them as errors, or document them with exception elements.",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Known exceptions should be returned through the Result error channel, explicitly allowed, or declared in the function's XML documentation.",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.KnownExceptionMayEscapeIdentifier)
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
            var state = new AnalyzerState(
                compilationContext.Compilation,
                compilationContext.Options.AdditionalFiles
            );
            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeThrow(operationContext, state),
                OperationKind.Throw
            );
            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeDocumentedMember(
                    operationContext,
                    state,
                    ((IInvocationOperation)operationContext.Operation).TargetMethod
                ),
                OperationKind.Invocation
            );
            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeDocumentedMember(
                    operationContext,
                    state,
                    ((IObjectCreationOperation)operationContext.Operation).Constructor
                ),
                OperationKind.ObjectCreation
            );
            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeDocumentedMember(
                    operationContext,
                    state,
                    ((IPropertyReferenceOperation)operationContext.Operation).Property
                ),
                OperationKind.PropertyReference
            );
        });
    }

    private static void AnalyzeThrow(OperationAnalysisContext context, AnalyzerState state)
    {
        IMethodSymbol? containingFunction = GetContainingFunction(context.Operation, context.ContainingSymbol);
        if (containingFunction is null || !containingFunction.ReturnType.IsResultReturningType())
            return;

        var operation = (IThrowOperation)context.Operation;
        INamedTypeSymbol? exceptionType = GetThrownExceptionType(operation, state.Compilation);
        if (
            exceptionType is null
            || IsPermitted(context, state, containingFunction, exceptionType, context.Operation)
        )
            return;

        ReportDiagnostic(context, operation, [exceptionType]);
    }

    private static void AnalyzeDocumentedMember(
        OperationAnalysisContext context,
        AnalyzerState state,
        ISymbol? invokedMember
    )
    {
        if (invokedMember is null || context.Operation.IsImplicit)
            return;
        IMethodSymbol? containingFunction = GetContainingFunction(context.Operation, context.ContainingSymbol);
        if (containingFunction is null || !containingFunction.ReturnType.IsResultReturningType())
            return;

        ImmutableArray<INamedTypeSymbol> documentedExceptions = state.GetDocumentedExceptions(invokedMember);
        if (documentedExceptions.IsDefaultOrEmpty)
            return;

        IOperation? catchSite = GetCatchSite(context.Operation, invokedMember);
        var escapingExceptions = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
        foreach (INamedTypeSymbol exceptionType in documentedExceptions)
        {
            if (!IsPermitted(context, state, containingFunction, exceptionType, catchSite))
                escapingExceptions.Add(exceptionType);
        }

        if (escapingExceptions.Count > 0)
            ReportDiagnostic(context, context.Operation, escapingExceptions.ToImmutable());
    }

    private static IMethodSymbol? GetContainingFunction(IOperation operation, ISymbol fallbackSymbol)
    {
        for (IOperation? current = operation.Parent; current is not null; current = current.Parent)
        {
            if (current is IAnonymousFunctionOperation anonymousFunction)
                return anonymousFunction.Symbol;
            if (current is ILocalFunctionOperation localFunction)
                return localFunction.Symbol;
        }
        return fallbackSymbol as IMethodSymbol;
    }

    private static INamedTypeSymbol? GetThrownExceptionType(IThrowOperation operation, Compilation compilation)
    {
        IOperation? exception = operation.Exception;
        while (exception is IConversionOperation { IsImplicit: true } conversion)
            exception = conversion.Operand;

        if (exception is { ConstantValue: { HasValue: true, Value: null } })
            return compilation.GetTypeByMetadataName("System.NullReferenceException");

        if (exception?.Type is INamedTypeSymbol exceptionType)
            return exceptionType;

        for (IOperation? current = operation.Parent; current is not null; current = current.Parent)
        {
            if (current is ICatchClauseOperation catchClause)
            {
                return catchClause.ExceptionType as INamedTypeSymbol
                    ?? compilation.GetTypeByMetadataName("System.Exception");
            }
            if (current is IAnonymousFunctionOperation or ILocalFunctionOperation)
                break;
        }

        return null;
    }

    private static bool IsPermitted(
        OperationAnalysisContext context,
        AnalyzerState state,
        IMethodSymbol containingFunction,
        INamedTypeSymbol exceptionType,
        IOperation? catchSite
    )
    {
        ImmutableArray<INamedTypeSymbol> configuredAllowedExceptions = state.GetConfiguredAllowedExceptions(
            context.Options.AnalyzerConfigOptionsProvider,
            context.Operation.Syntax.SyntaxTree
        );
        if (IsCoveredBy(exceptionType, configuredAllowedExceptions))
            return true;

        ISymbol documentationOwner = GetDocumentationOwner(containingFunction);
        if (IsCoveredBy(exceptionType, state.GetDocumentedExceptions(documentationOwner)))
            return true;

        return catchSite is not null && IsCaught(catchSite, exceptionType, state.Compilation);
    }

    private static IOperation? GetCatchSite(IOperation operation, ISymbol invokedMember)
    {
        if (invokedMember is not IMethodSymbol method || !method.ReturnType.IsTaskLike())
            return operation;

        IOperation current = operation;
        while (current.Parent is { } parent)
        {
            switch (parent)
            {
                case IConversionOperation { IsImplicit: true }:
                case IParenthesizedOperation:
                    current = parent;
                    continue;
                case IInvocationOperation configureAwait
                    when ReferenceEquals(configureAwait.Instance, current)
                        && configureAwait.TargetMethod.Name == "ConfigureAwait"
                        && configureAwait.TargetMethod.ContainingType.IsTaskLike():
                    current = configureAwait;
                    continue;
                case IAwaitOperation awaitOperation:
                    return awaitOperation;
                default:
                    return null;
            }
        }
        return null;
    }

    private static ISymbol GetDocumentationOwner(ISymbol symbol)
    {
        return symbol is IMethodSymbol { AssociatedSymbol: { } associatedSymbol } ? associatedSymbol : symbol;
    }

    private static bool IsCoveredBy(
        INamedTypeSymbol exceptionType,
        ImmutableArray<INamedTypeSymbol> permittedBaseTypes
    )
    {
        foreach (INamedTypeSymbol permittedBaseType in permittedBaseTypes)
        {
            if (IsSameOrDerivedFrom(exceptionType, permittedBaseType))
                return true;
        }
        return false;
    }

    private static bool IsCaught(IOperation operation, INamedTypeSymbol exceptionType, Compilation compilation)
    {
        IOperation child = operation;
        for (IOperation? current = operation.Parent; current is not null; child = current, current = current.Parent)
        {
            if (current is IAnonymousFunctionOperation or ILocalFunctionOperation)
                return false;
            if (current is not ITryOperation tryOperation || !ReferenceEquals(child, tryOperation.Body))
                continue;

            foreach (ICatchClauseOperation catchClause in tryOperation.Catches)
            {
                if (catchClause.Filter is not null)
                    continue;
                INamedTypeSymbol? caughtType = catchClause.ExceptionType as INamedTypeSymbol
                    ?? compilation.GetTypeByMetadataName("System.Exception");
                if (caughtType is not null && IsSameOrDerivedFrom(exceptionType, caughtType))
                    return true;
            }
        }

        return false;
    }

    private static bool IsSameOrDerivedFrom(INamedTypeSymbol type, INamedTypeSymbol possibleBaseType)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, possibleBaseType.OriginalDefinition))
                return true;
        }
        return false;
    }

    private static void ReportDiagnostic(
        OperationAnalysisContext context,
        IOperation operation,
        ImmutableArray<INamedTypeSymbol> exceptionTypes
    )
    {
        string[] displayNames = exceptionTypes
            .Select(type => type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        string[] documentationIds = exceptionTypes
            .Select(DocumentationCommentId.CreateDeclarationId)
            .Where(id => id is not null)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray()!;
        ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty.Add(
            "ExceptionTypes",
            string.Join(";", documentationIds)
        );
        context.ReportDiagnostic(
            Diagnostic.Create(s_rule, operation.Syntax.GetLocation(), properties, string.Join(", ", displayNames))
        );
    }

    private sealed class AnalyzerState(Compilation compilation, ImmutableArray<AdditionalText> additionalFiles)
    {
        private readonly ConcurrentDictionary<ISymbol, ImmutableArray<INamedTypeSymbol>> _documentedExceptions =
            new(SymbolEqualityComparer.Default);
        private readonly ConcurrentDictionary<SyntaxTree, ImmutableArray<INamedTypeSymbol>> _configuredAllowedExceptions =
            new();
        private readonly ConcurrentDictionary<AdditionalText, ImmutableDictionary<string, ImmutableArray<INamedTypeSymbol>>> _additionalDocumentation =
            new();
        private readonly Dictionary<string, ImmutableArray<AdditionalText>> _additionalDocumentationByAssembly =
            CreateAdditionalDocumentationMap(additionalFiles);
        private readonly INamedTypeSymbol? _exceptionType = compilation.GetTypeByMetadataName("System.Exception");

        public Compilation Compilation { get; } = compilation;

        public ImmutableArray<INamedTypeSymbol> GetDocumentedExceptions(ISymbol symbol)
        {
            return _documentedExceptions.GetOrAdd(symbol, ReadDocumentedExceptions);
        }

        public ImmutableArray<INamedTypeSymbol> GetConfiguredAllowedExceptions(
            AnalyzerConfigOptionsProvider optionsProvider,
            SyntaxTree syntaxTree
        )
        {
            return _configuredAllowedExceptions.GetOrAdd(
                syntaxTree,
                tree =>
                {
                    AnalyzerConfigOptions options = optionsProvider.GetOptions(tree);
                    IEnumerable<string> configuredTypeNames = s_defaultAllowedExceptionTypeNames;
                    if (options.TryGetValue(AllowedExceptionTypesOption, out string? configuredValue))
                        configuredTypeNames = configuredValue.Split(';');

                    var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
                    foreach (string configuredTypeName in configuredTypeNames)
                    {
                        string metadataName = configuredTypeName.Trim();
                        if (metadataName.StartsWith("global::", StringComparison.Ordinal))
                            metadataName = metadataName.Substring("global::".Length);
                        if (metadataName.StartsWith("T:", StringComparison.Ordinal))
                            metadataName = metadataName.Substring("T:".Length);
                        if (
                            Compilation.GetTypeByMetadataName(metadataName) is { } type
                            && _exceptionType is not null
                            && IsSameOrDerivedFrom(type, _exceptionType)
                        )
                        {
                            AddDistinct(builder, type);
                        }
                    }
                    return builder.ToImmutable();
                }
            );
        }

        private ImmutableArray<INamedTypeSymbol> ReadDocumentedExceptions(ISymbol symbol)
        {
            var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
            foreach (ISymbol candidate in GetDocumentationCandidates(symbol))
            {
                string? xml = candidate.GetDocumentationCommentXml(cancellationToken: default);
                if (!string.IsNullOrWhiteSpace(xml))
                {
                    try
                    {
                        foreach (XElement exceptionElement in XElement.Parse(xml).Descendants("exception"))
                        {
                            string? declarationId = exceptionElement.Attribute("cref")?.Value;
                            if (declarationId is null || declarationId.StartsWith("!:", StringComparison.Ordinal))
                                continue;
                            if (
                                DocumentationCommentId.GetFirstSymbolForDeclarationId(declarationId, Compilation)
                                is INamedTypeSymbol exceptionType
                                && _exceptionType is not null
                                && IsSameOrDerivedFrom(exceptionType, _exceptionType)
                            )
                            {
                                AddDistinct(builder, exceptionType);
                            }
                        }
                    }
                    catch (XmlException)
                    {
                        // Invalid XML documentation cannot establish an exception contract.
                    }
                }

                ReadAdditionalDocumentation(candidate, builder);
            }
            return builder.ToImmutable();
        }

        private void ReadAdditionalDocumentation(
            ISymbol symbol,
            ImmutableArray<INamedTypeSymbol>.Builder builder
        )
        {
            if (
                symbol.ContainingAssembly is not { } containingAssembly
                || !_additionalDocumentationByAssembly.TryGetValue(containingAssembly.Name, out var documentationFiles)
                || symbol.GetDocumentationCommentId() is not { } declarationId
            )
            {
                return;
            }

            foreach (AdditionalText documentationFile in documentationFiles)
            {
                ImmutableDictionary<string, ImmutableArray<INamedTypeSymbol>> documentation =
                    _additionalDocumentation.GetOrAdd(documentationFile, ParseAdditionalDocumentation);
                if (!documentation.TryGetValue(declarationId, out var exceptionTypes))
                    continue;
                foreach (INamedTypeSymbol exceptionType in exceptionTypes)
                    AddDistinct(builder, exceptionType);
            }
        }

        private ImmutableDictionary<string, ImmutableArray<INamedTypeSymbol>> ParseAdditionalDocumentation(
            AdditionalText documentationFile
        )
        {
            string? xml = documentationFile.GetText()?.ToString();
            if (string.IsNullOrWhiteSpace(xml))
                return ImmutableDictionary<string, ImmutableArray<INamedTypeSymbol>>.Empty;

            try
            {
                var documentation = ImmutableDictionary.CreateBuilder<string, ImmutableArray<INamedTypeSymbol>>(
                    StringComparer.Ordinal
                );
                foreach (XElement memberElement in XElement.Parse(xml).Descendants("member"))
                {
                    string? declarationId = memberElement.Attribute("name")?.Value;
                    if (declarationId is null)
                        continue;

                    var exceptionTypes = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
                    foreach (XElement exceptionElement in memberElement.Elements("exception"))
                    {
                        string? exceptionId = exceptionElement.Attribute("cref")?.Value;
                        if (
                            exceptionId is not null
                            && !exceptionId.StartsWith("!:", StringComparison.Ordinal)
                            && DocumentationCommentId.GetFirstSymbolForDeclarationId(exceptionId, Compilation)
                                is INamedTypeSymbol exceptionType
                            && _exceptionType is not null
                            && IsSameOrDerivedFrom(exceptionType, _exceptionType)
                        )
                        {
                            AddDistinct(exceptionTypes, exceptionType);
                        }
                    }

                    if (exceptionTypes.Count > 0)
                        documentation[declarationId] = exceptionTypes.ToImmutable();
                }
                return documentation.ToImmutable();
            }
            catch (XmlException)
            {
                return ImmutableDictionary<string, ImmutableArray<INamedTypeSymbol>>.Empty;
            }
        }

        private static Dictionary<string, ImmutableArray<AdditionalText>> CreateAdditionalDocumentationMap(
            ImmutableArray<AdditionalText> additionalFiles
        )
        {
            var builders = new Dictionary<string, ImmutableArray<AdditionalText>.Builder>(
                StringComparer.OrdinalIgnoreCase
            );
            foreach (AdditionalText additionalFile in additionalFiles)
            {
                string assemblyName = GetFileNameWithoutExtension(additionalFile.Path);
                if (assemblyName.Length == 0)
                    continue;

                if (!builders.TryGetValue(assemblyName, out var builder))
                {
                    builder = ImmutableArray.CreateBuilder<AdditionalText>();
                    builders.Add(assemblyName, builder);
                }
                builder.Add(additionalFile);
            }

            var documentationByAssembly = new Dictionary<string, ImmutableArray<AdditionalText>>(
                StringComparer.OrdinalIgnoreCase
            );
            foreach (KeyValuePair<string, ImmutableArray<AdditionalText>.Builder> entry in builders)
                documentationByAssembly.Add(entry.Key, entry.Value.ToImmutable());
            return documentationByAssembly;
        }

        private static string GetFileNameWithoutExtension(string path)
        {
            int separatorIndex = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
            int extensionIndex = path.LastIndexOf('.');
            int startIndex = separatorIndex + 1;
            int endIndex = extensionIndex > separatorIndex ? extensionIndex : path.Length;
            return endIndex > startIndex ? path.Substring(startIndex, endIndex - startIndex) : string.Empty;
        }

        private static IEnumerable<ISymbol> GetDocumentationCandidates(ISymbol symbol)
        {
            symbol = GetDocumentationOwner(symbol);
            yield return symbol;

            switch (symbol)
            {
                case IMethodSymbol method:
                    if (method.ReducedFrom is { } reducedFrom)
                    {
                        yield return reducedFrom;
                        if (!SymbolEqualityComparer.Default.Equals(reducedFrom, reducedFrom.OriginalDefinition))
                            yield return reducedFrom.OriginalDefinition;
                    }
                    if (!SymbolEqualityComparer.Default.Equals(method, method.OriginalDefinition))
                        yield return method.OriginalDefinition;
                    for (IMethodSymbol? overridden = method.OverriddenMethod; overridden is not null; overridden = overridden.OverriddenMethod)
                        yield return overridden;
                    foreach (IMethodSymbol implementation in method.ExplicitInterfaceImplementations)
                        yield return implementation;
                    foreach (ISymbol contract in GetImplicitInterfaceContracts(method))
                        yield return contract;
                    break;
                case IPropertySymbol property:
                    if (!SymbolEqualityComparer.Default.Equals(property, property.OriginalDefinition))
                        yield return property.OriginalDefinition;
                    for (IPropertySymbol? overridden = property.OverriddenProperty; overridden is not null; overridden = overridden.OverriddenProperty)
                        yield return overridden;
                    foreach (IPropertySymbol implementation in property.ExplicitInterfaceImplementations)
                        yield return implementation;
                    foreach (ISymbol contract in GetImplicitInterfaceContracts(property))
                        yield return contract;
                    break;
            }
        }

        private static IEnumerable<ISymbol> GetImplicitInterfaceContracts(ISymbol symbol)
        {
            foreach (INamedTypeSymbol interfaceType in symbol.ContainingType.AllInterfaces)
            {
                foreach (ISymbol interfaceMember in interfaceType.GetMembers(symbol.Name))
                {
                    ISymbol? implementation = symbol.ContainingType.FindImplementationForInterfaceMember(
                        interfaceMember
                    );
                    if (SymbolEqualityComparer.Default.Equals(implementation, symbol))
                        yield return interfaceMember;
                }
            }
        }

        private static void AddDistinct(
            ImmutableArray<INamedTypeSymbol>.Builder builder,
            INamedTypeSymbol exceptionType
        )
        {
            foreach (INamedTypeSymbol existingType in builder)
            {
                if (SymbolEqualityComparer.Default.Equals(existingType, exceptionType))
                    return;
            }
            builder.Add(exceptionType);
        }
    }
}
