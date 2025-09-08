using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Darp.Results.Analyzers;

internal static class Helpers
{
    public const string ResultNamespace = "Darp.Results";
    public const string ResultName = "Result";
    public const string ResultErrName = "Err";
    public const string ResultOkName = "Ok";

    public static bool IsOrExtendsResult([NotNullWhen(true)] this ITypeSymbol? type) => type.IsOrExtendsResult(out _);

    public static bool IsOrExtendsResult([NotNullWhen(true)] this ITypeSymbol? type, out bool isWrappedInTask)
    {
        while (type is INamedTypeSymbol named)
        {
            switch (named)
            {
                case { Arity: 2, OriginalDefinition.Name: ResultName }
                    when named.ContainingNamespace?.ToDisplayString() == ResultNamespace:
                    isWrappedInTask = false;
                    return true;
                case { Arity: 1, OriginalDefinition.Name: "Task" or "ValueTask" }
                    when named.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks":
                    isWrappedInTask = true;
                    return named.TypeArguments[0].IsOrExtendsResult(out _);
                default:
                    type = named.BaseType;
                    break;
            }
        }
        isWrappedInTask = false;
        return false;
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

    public static bool IsUnused(this IInvocationOperation invocation, bool isTask, bool countDiscardsAsUnused = false)
    {
        // Climb over implicit conversions.
        IOperation node = invocation;
        while (node.Parent is IConversionOperation { IsImplicit: true } conv)
            node = conv;

        // Handle `await Call();` — the invocation is inside an await node.
        if (isTask)
        {
            if (node.Parent is not IAwaitOperation awaitOp)
                return false;
            node = awaitOp;
            // Skip implicit conversions above await, if any.
            while (node.Parent is IConversionOperation { IsImplicit: true } conv2)
                node = conv2;
        }

        // Case 1: Used directly as a statement => unused.
        if (node.Parent is IExpressionStatementOperation)
            return true;

        // Case 2: `_ = Call();`
        if (countDiscardsAsUnused && node.Parent is ISimpleAssignmentOperation { Target: IDiscardOperation })
        {
            return true;
        }

        // Otherwise, it's being assigned, returned, passed as an argument, etc.
        return false;
    }

    public static string GetResultCaseName(ITypeSymbol resultType, string resultCase)
    {
        if (resultType is not INamedTypeSymbol namedTypeSymbol || namedTypeSymbol.TypeArguments.Length != 2)
            return $"Result.{resultCase}<,>";
        return $"Result.{resultCase}<{namedTypeSymbol.TypeArguments[0]}, {namedTypeSymbol.TypeArguments[1]}>";
    }
}
