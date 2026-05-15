using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Darp.Results.Analyzers;

internal static class Helpers
{
    public const string ResultNamespace = "Darp.Results";
    public const string ResultName = "Result";
    public const string ResultErrName = "Err";
    public const string ResultOkName = "Ok";

    public static bool IsResult([NotNullWhen(true)] this ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named)
            return false;
        if (
            named is { Arity: 2, OriginalDefinition.Name: ResultName }
            && named.ContainingNamespace?.ToDisplayString() == ResultNamespace
        )
        {
            return true;
        }
        return named.BaseType
                is { Arity: 2, OriginalDefinition.Name: ResultName, ContainingNamespace: { } containingNamespace }
            && containingNamespace.ToDisplayString() == ResultNamespace;
    }

    public static bool IsErrorResult([NotNullWhen(true)] this ITypeSymbol? type, ITypeSymbol resultType)
    {
        if (type is not { Name: ResultErrName })
            return false;
        return SymbolEqualityComparer.Default.Equals(type.BaseType, resultType);
    }

    public static bool IsOkResult([NotNullWhen(true)] this ITypeSymbol? type, ITypeSymbol resultType)
    {
        if (type is not { Name: ResultOkName })
            return false;
        return SymbolEqualityComparer.Default.Equals(type.BaseType, resultType);
    }

    public static string GetResultCaseName(ITypeSymbol resultType, string resultCase)
    {
        if (resultType is not INamedTypeSymbol namedTypeSymbol || namedTypeSymbol.TypeArguments.Length != 2)
            return $"Result.{resultCase}<,>";
        return $"Result.{resultCase}<{namedTypeSymbol.TypeArguments[0]}, {namedTypeSymbol.TypeArguments[1]}>";
    }
}
