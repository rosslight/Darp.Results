using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Darp.Results.Analyzers;

internal static class Helpers
{
    public const string ResultNamespace = "Darp.Results";
    public const string ResultName = "Result";

    public static bool IsResult([NotNullWhen(true)] this ITypeSymbol? type)
    {
        return type is INamedTypeSymbol { Arity: 2, OriginalDefinition.Name: ResultName } named
            && named.ContainingNamespace?.ToDisplayString() == ResultNamespace;
    }

    public static bool IsResultReturningType(this ITypeSymbol? type)
    {
        if (type.IsResult())
            return true;
        if (type is not INamedTypeSymbol { Arity: 1 } named || !named.IsTaskLike())
            return false;
        return named.TypeArguments[0].IsResult();
    }

    public static bool IsTaskLike(this ITypeSymbol? type)
    {
        return type is INamedTypeSymbol { Arity: 0 or 1 } named
            && named.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks"
            && named.OriginalDefinition.Name is "Task" or "ValueTask";
    }
}
