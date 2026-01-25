using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
///     Handles CTE (Common Table Expression) related code generation.
/// </summary>
public static class CteEmitter
{
    /// <summary>
    ///     Creates the assignment statement for CTE inner expression result storage.
    /// </summary>
    /// <param name="cteName">The CTE name to look up in the table indexes.</param>
    /// <param name="tableIndex">The index of the table in the _tableResults array.</param>
    /// <param name="methodName">The method name to invoke for computing the CTE.</param>
    /// <returns>An expression statement that assigns the method result to _tableResults.</returns>
    public static ExpressionStatementSyntax CreateCteInnerExpressionAssignment(
        string cteName,
        int tableIndex,
        string methodName)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateTableResultsAccess(tableIndex),
                CreateMethodInvocation(methodName)));
    }

    /// <summary>
    ///     Creates element access expression for _tableResults array.
    /// </summary>
    private static ElementAccessExpressionSyntax CreateTableResultsAccess(int tableIndex)
    {
        return SyntaxFactory.ElementAccessExpression(
                SyntaxFactory.IdentifierName("_tableResults"))
            .WithArgumentList(
                SyntaxFactory.BracketedArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(tableIndex))))));
    }

    /// <summary>
    ///     Creates the method invocation with standard CTE parameters.
    /// </summary>
    private static InvocationExpressionSyntax CreateMethodInvocation(string methodName)
    {
        var arguments = new[]
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("provider")),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("queriesInformation")),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("logger")),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("token"))
        };

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName(methodName))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(arguments)));
    }

    /// <summary>
    ///     Registers a CTE name in the table indexes if not already present.
    /// </summary>
    /// <param name="cteName">The CTE name to register.</param>
    /// <param name="tableIndexes">The dictionary mapping CTE names to indexes.</param>
    /// <param name="currentIndex">The current table index counter.</param>
    /// <returns>True if a new index was assigned, false if already registered.</returns>
    public static bool TryRegisterCteIndex(
        string cteName,
        Dictionary<string, int> tableIndexes,
        ref int currentIndex)
    {
        if (tableIndexes.ContainsKey(cteName))
            return false;

        tableIndexes.Add(cteName, currentIndex++);
        return true;
    }

    /// <summary>
    ///     Gets the table index for a registered CTE name.
    /// </summary>
    public static int GetCteIndex(string cteName, Dictionary<string, int> tableIndexes)
    {
        return tableIndexes[cteName];
    }

    /// <summary>
    ///     Creates an element access expression for referencing a CTE result in _tableResults.
    /// </summary>
    /// <param name="tableIndex">The index of the table in _tableResults.</param>
    /// <returns>An element access expression for the CTE reference.</returns>
    public static ElementAccessExpressionSyntax CreateCteReference(int tableIndex)
    {
        return SyntaxFactory.ElementAccessExpression(
                SyntaxFactory.IdentifierName("_tableResults"))
            .WithArgumentList(
                SyntaxFactory.BracketedArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(tableIndex))))));
    }
}
