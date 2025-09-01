using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Darp.Results.Analyzers;

internal static class Helpers
{
    public const string ResultNamespace = "Darp.Results";
    public const string ResultName = "Result";

    public static bool IsOrExtendsResult([NotNullWhen(true)] this ITypeSymbol? type)
    {
        while (type is INamedTypeSymbol named)
        {
            if (
                named is { Arity: 2, OriginalDefinition.Name: ResultName }
                && named.ContainingNamespace?.ToDisplayString() == ResultNamespace
            )
            {
                return true;
            }

            type = named.BaseType;
        }
        return false;
    }

    public static bool IsUnused(this IInvocationOperation invocation, bool includeDiscardAssignments = true)
    {
        // Climb over implicit conversions.
        IOperation node = invocation;
        while (node.Parent is IConversionOperation { IsImplicit: true } conv)
            node = conv;

        // Handle `await Call();` — the invocation is inside an await node.
        if (node.Parent is IAwaitOperation awaitOp)
        {
            node = awaitOp;
            // Skip implicit conversions above await, if any.
            while (node.Parent is IConversionOperation conv2 && conv2.IsImplicit)
                node = conv2;
        }

        // Case 1: Used directly as a statement => unused.
        if (node.Parent is IExpressionStatementOperation)
            return true;

        // Case 2: `_ = Call();`
        if (includeDiscardAssignments && node.Parent is ISimpleAssignmentOperation { Target: IDiscardOperation })
        {
            return true;
        }

        // Otherwise, it's being assigned, returned, passed as an argument, etc.
        return false;
    }
}
