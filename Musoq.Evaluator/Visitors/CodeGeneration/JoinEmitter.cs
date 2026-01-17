using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
///     Handles code generation for JOIN-related operations.
/// </summary>
public static class JoinEmitter
{
    /// <summary>
    ///     Creates the standard join condition check if statement.
    ///     This is used across different join node visitors.
    /// </summary>
    /// <param name="conditionExpression">The join condition expression (already negated).</param>
    /// <param name="generator">The Roslyn syntax generator.</param>
    /// <returns>A statement syntax for the join condition check.</returns>
    public static StatementSyntax CreateJoinConditionCheck(SyntaxNode conditionExpression, SyntaxGenerator generator)
    {
        return ((StatementSyntax)generator.IfStatement(
                generator.LogicalNotExpression(conditionExpression),
                [StatementEmitter.CreateContinue()]))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
    }
}