using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Darp.Results.Analyzers;

internal static class Helpers
{
    public const string ResultNamespace = "Darp.Results";
    public const string ResultName = "Result";
    public const string OkName = "Ok";
    public const string ErrName = "Err";

    /// <summary> True for <c>Result&lt;TValue, TError&gt;</c> and its cases <c>Ok&lt;TValue&gt;</c> and <c>Err&lt;TError&gt;</c>. </summary>
    public static bool IsResult([NotNullWhen(true)] this ITypeSymbol? type)
    {
        return type is INamedTypeSymbol named
            && named.OriginalDefinition switch
            {
                { Arity: 2, Name: ResultName } => true,
                { Arity: 1, Name: OkName or ErrName } => true,
                _ => false,
            }
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
