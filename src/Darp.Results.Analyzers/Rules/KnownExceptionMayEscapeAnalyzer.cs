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
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.KnownExceptionMayEscapeIdentifier,
        title: "Explicit exception may escape a Result-returning function",
        messageFormat: "The following exception types may escape this Result-returning function: {0}. Catch and return them as errors, or document them with exception elements.",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Explicitly thrown exceptions should be returned through the Result error channel, explicitly allowed, or declared in the function's XML documentation.",
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
            var state = new ExceptionEscapeAnalysis.AnalyzerState(
                compilationContext.Compilation,
                compilationContext.Options.AdditionalFiles
            );
            compilationContext.RegisterOperationAction(
                operationContext => ExceptionEscapeAnalysis.AnalyzeThrow(operationContext, state, s_rule),
                OperationKind.Throw
            );
        });
    }
}

internal static class ExceptionEscapeAnalysis
{
    private const string AllowedExceptionTypesOption =
        "dotnet_code_quality.darp_results_allowed_exception_types";
    private const string ExcludedMembersOption =
        "dotnet_code_quality." + RuleIdentifiers.DocumentedExceptionMayEscapeIdentifier + ".excluded_members";

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

    internal static void AnalyzeThrow(
        OperationAnalysisContext context,
        AnalyzerState state,
        DiagnosticDescriptor rule
    )
    {
        IMethodSymbol? containingFunction = GetContainingFunction(context.Operation, context.ContainingSymbol);
        if (containingFunction is null || !containingFunction.ReturnType.IsResultReturningType())
            return;

        var operation = (IThrowOperation)context.Operation;
        INamedTypeSymbol? exceptionType = GetThrownExceptionType(operation, state.Compilation);
        if (exceptionType is null || IsPermitted(context, state, containingFunction, exceptionType, context.Operation))
            return;

        ReportDiagnostic(context, operation, [exceptionType], rule);
    }

    internal static void AnalyzeDocumentedMember(
        OperationAnalysisContext context,
        AnalyzerState state,
        DiagnosticDescriptor rule
    )
    {
        if (IsInsideNameOf(context.Operation))
            return;

        (ISymbol? invokedMember, INamedTypeSymbol? receiverType) = GetInvokedMember(context.Operation);
        if (
            invokedMember is null
            || context.Operation.IsImplicit
                && context.Operation is not IConversionOperation { OperatorMethod: not null }
        )
            return;
        IMethodSymbol? containingFunction = GetContainingFunction(context.Operation, context.ContainingSymbol);
        if (containingFunction is null || !containingFunction.ReturnType.IsResultReturningType())
            return;
        if (
            state.IsExcludedMember(
                context.Options.AnalyzerConfigOptionsProvider,
                context.Operation.Syntax.SyntaxTree,
                invokedMember,
                receiverType
            )
        )
        {
            return;
        }

        ImmutableArray<INamedTypeSymbol> documentedExceptions = GetDocumentedExceptions(
            state,
            invokedMember,
            receiverType
        );
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
            ReportDiagnostic(context, context.Operation, escapingExceptions.ToImmutable(), rule);
    }

    private static bool IsInsideNameOf(IOperation operation)
    {
        for (IOperation? current = operation.Parent; current is not null; current = current.Parent)
        {
            if (current is INameOfOperation)
                return true;
        }
        return false;
    }

    private static (ISymbol? Member, INamedTypeSymbol? ReceiverType) GetInvokedMember(IOperation operation)
    {
        return operation switch
        {
            IInvocationOperation invocation => (invocation.TargetMethod, invocation.Instance?.Type as INamedTypeSymbol),
            IObjectCreationOperation creation => (creation.Constructor, null),
            IPropertyReferenceOperation property => (property.Property, property.Instance?.Type as INamedTypeSymbol),
            IBinaryOperation binary => (binary.OperatorMethod, null),
            IUnaryOperation unary => (unary.OperatorMethod, null),
            IConversionOperation conversion => (conversion.OperatorMethod, null),
            ICompoundAssignmentOperation assignment => (assignment.OperatorMethod, null),
            IIncrementOrDecrementOperation increment => (increment.OperatorMethod, null),
            _ => (null, null),
        };
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
        if (
            IsCoveredBy(
                exceptionType,
                GetDocumentedExceptions(state, documentationOwner, containingFunction.ContainingType)
            )
        )
            return true;

        return catchSite is not null && IsCaught(catchSite, exceptionType, state.Compilation);
    }

    private static IOperation? GetCatchSite(IOperation operation, ISymbol invokedMember)
    {
        ITypeSymbol? resultType = invokedMember switch
        {
            IMethodSymbol method => method.ReturnType,
            IPropertySymbol property => property.Type,
            _ => null,
        };
        if (resultType is null || !resultType.IsTaskLike())
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

    private static ImmutableArray<INamedTypeSymbol> GetDocumentedExceptions(
        AnalyzerState state,
        ISymbol symbol,
        INamedTypeSymbol? receiverType
    )
    {
        var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
        foreach (ISymbol candidate in GetDocumentationCandidates(symbol, receiverType))
        {
            foreach (INamedTypeSymbol exceptionType in state.GetDeclaredExceptions(candidate))
                AddDistinct(builder, exceptionType);
        }
        return builder.ToImmutable();
    }

    private static IEnumerable<ISymbol> GetDocumentationCandidates(ISymbol symbol, INamedTypeSymbol? receiverType)
    {
        var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (ISymbol contract in GetApplicableContracts(GetDocumentationOwner(symbol), receiverType))
        {
            ISymbol definition = contract.OriginalDefinition;
            if (seen.Add(definition))
                yield return definition;
        }
    }

    private static IEnumerable<ISymbol> GetApplicableContracts(ISymbol symbol, INamedTypeSymbol? receiverType)
    {
        yield return symbol;

        switch (symbol)
        {
            case IMethodSymbol method:
                if (method.ReducedFrom is { } reducedFrom)
                    yield return reducedFrom;
                for (
                    IMethodSymbol? overridden = method.OverriddenMethod;
                    overridden is not null;
                    overridden = overridden.OverriddenMethod
                )
                {
                    yield return overridden;
                }
                foreach (IMethodSymbol contract in method.ExplicitInterfaceImplementations)
                    yield return contract;
                break;
            case IPropertySymbol property:
                for (
                    IPropertySymbol? overridden = property.OverriddenProperty;
                    overridden is not null;
                    overridden = overridden.OverriddenProperty
                )
                {
                    yield return overridden;
                }
                foreach (IPropertySymbol contract in property.ExplicitInterfaceImplementations)
                    yield return contract;
                break;
        }

        foreach (ISymbol contract in GetImplicitInterfaceContracts(symbol, receiverType ?? symbol.ContainingType))
            yield return contract;
    }

    private static IEnumerable<ISymbol> GetImplicitInterfaceContracts(ISymbol symbol, INamedTypeSymbol? receiverType)
    {
        if (receiverType is null)
            yield break;

        foreach (INamedTypeSymbol interfaceType in receiverType.AllInterfaces)
        {
            foreach (ISymbol interfaceMember in interfaceType.GetMembers(symbol.Name))
            {
                ISymbol? implementation = receiverType.FindImplementationForInterfaceMember(interfaceMember);
                if (
                    implementation is not null
                    && SymbolEqualityComparer.Default.Equals(
                        implementation.OriginalDefinition,
                        symbol.OriginalDefinition
                    )
                )
                {
                    yield return interfaceMember;
                }
            }
        }
    }

    private static bool IsCoveredBy(INamedTypeSymbol exceptionType, ImmutableArray<INamedTypeSymbol> permittedBaseTypes)
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
                INamedTypeSymbol? caughtType =
                    catchClause.ExceptionType as INamedTypeSymbol
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
        ImmutableArray<INamedTypeSymbol> exceptionTypes,
        DiagnosticDescriptor rule
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
            Diagnostic.Create(rule, operation.Syntax.GetLocation(), properties, string.Join(", ", displayNames))
        );
    }

    private static void AddDistinct(ImmutableArray<INamedTypeSymbol>.Builder builder, INamedTypeSymbol exceptionType)
    {
        foreach (INamedTypeSymbol existingType in builder)
        {
            if (SymbolEqualityComparer.Default.Equals(existingType, exceptionType))
                return;
        }
        builder.Add(exceptionType);
    }

    internal sealed class AnalyzerState(Compilation compilation, ImmutableArray<AdditionalText> additionalFiles)
    {
        private readonly ConcurrentDictionary<ISymbol, ImmutableArray<INamedTypeSymbol>> _declaredExceptions = new(
            SymbolEqualityComparer.Default
        );
        private readonly ConcurrentDictionary<
            SyntaxTree,
            ImmutableArray<INamedTypeSymbol>
        > _configuredAllowedExceptions = new();
        private readonly ConcurrentDictionary<SyntaxTree, ImmutableHashSet<string>> _configuredExcludedMembers = new();
        private readonly Dictionary<string, ImmutableArray<AdditionalText>> _additionalDocumentationByAssembly =
            CreateAdditionalDocumentationMap(additionalFiles);
        private readonly INamedTypeSymbol? _exceptionType = compilation.GetTypeByMetadataName("System.Exception");

        public Compilation Compilation { get; } = compilation;

        public ImmutableArray<INamedTypeSymbol> GetDeclaredExceptions(ISymbol symbol)
        {
            return _declaredExceptions.GetOrAdd(symbol, ReadDeclaredExceptions);
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
                        configuredTypeNames = configuredValue.Split('|', ';');

                    var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
                    foreach (string configuredTypeName in configuredTypeNames)
                    {
                        if (
                            GetConfiguredExceptionType(configuredTypeName) is { } type
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

        public bool IsExcludedMember(
            AnalyzerConfigOptionsProvider optionsProvider,
            SyntaxTree syntaxTree,
            ISymbol symbol,
            INamedTypeSymbol? receiverType
        )
        {
            ImmutableHashSet<string> excludedMembers = _configuredExcludedMembers.GetOrAdd(
                syntaxTree,
                tree =>
                {
                    AnalyzerConfigOptions options = optionsProvider.GetOptions(tree);
                    if (!options.TryGetValue(ExcludedMembersOption, out string? configuredValue))
                        return ImmutableHashSet<string>.Empty;

                    return configuredValue
                        .Split('|')
                        .Select(member => member.Trim())
                        .Where(member => member.Length > 0)
                        .ToImmutableHashSet(StringComparer.Ordinal);
                }
            );
            if (excludedMembers.Count == 0)
                return false;

            foreach (ISymbol candidate in GetDocumentationCandidates(symbol, receiverType))
            {
                if (
                    candidate.GetDocumentationCommentId() is { } documentationId
                    && excludedMembers.Contains(documentationId)
                )
                {
                    return true;
                }
            }
            return false;
        }

        private INamedTypeSymbol? GetConfiguredExceptionType(string configuredTypeName)
        {
            string typeName = configuredTypeName.Trim();
            if (typeName.StartsWith("global::", StringComparison.Ordinal))
                typeName = typeName.Substring("global::".Length);

            string declarationId = typeName.StartsWith("T:", StringComparison.Ordinal) ? typeName : "T:" + typeName;
            string metadataName = typeName.StartsWith("T:", StringComparison.Ordinal)
                ? typeName.Substring("T:".Length)
                : typeName;
            return DocumentationCommentId.GetFirstSymbolForDeclarationId(declarationId, Compilation) as INamedTypeSymbol
                ?? Compilation.GetTypeByMetadataName(metadataName);
        }

        private ImmutableArray<INamedTypeSymbol> ReadDeclaredExceptions(ISymbol symbol)
        {
            var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
            string? xml = symbol.GetDocumentationCommentXml(cancellationToken: default);
            if (!string.IsNullOrWhiteSpace(xml))
            {
                try
                {
                    foreach (XElement exceptionElement in XElement.Parse(xml).Descendants("exception"))
                    {
                        AddExceptionType(exceptionElement.Attribute("cref")?.Value, builder);
                    }
                }
                catch (XmlException)
                {
                    // Invalid XML documentation cannot establish an exception contract.
                }
            }

            ReadAdditionalDocumentation(symbol, builder);
            return builder.ToImmutable();
        }

        private void ReadAdditionalDocumentation(ISymbol symbol, ImmutableArray<INamedTypeSymbol>.Builder builder)
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
                foreach (string exceptionId in XmlDocumentationIndex.GetExceptionIds(documentationFile, declarationId))
                {
                    AddExceptionType(exceptionId, builder);
                }
            }
        }

        private void AddExceptionType(string? declarationId, ImmutableArray<INamedTypeSymbol>.Builder builder)
        {
            if (
                declarationId is not null
                && !declarationId.StartsWith("!:", StringComparison.Ordinal)
                && DocumentationCommentId.GetFirstSymbolForDeclarationId(declarationId, Compilation)
                    is INamedTypeSymbol exceptionType
                && _exceptionType is not null
                && IsSameOrDerivedFrom(exceptionType, _exceptionType)
            )
            {
                AddDistinct(builder, exceptionType);
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
    }
}
