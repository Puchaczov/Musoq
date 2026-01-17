using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
///     Handles code generation for WHERE clause processing.
/// </summary>
public static class WhereEmitter
{
    /// <summary>
    ///     Creates a WHERE condition check that either continues or returns based on context.
    /// </summary>
    /// <param name="conditionExpression">The WHERE condition expression.</param>
    /// <param name="isParallelizationImpossible">Whether result parallelization is impossible.</param>
    /// <param name="isResultQuery">Whether this is a result query (vs transforming query).</param>
    /// <param name="generator">The Roslyn syntax generator.</param>
    /// <returns>A syntax node representing the WHERE condition check.</returns>
    public static SyntaxNode CreateWhereCondition(
        SyntaxNode conditionExpression,
        bool isParallelizationImpossible,
        bool isResultQuery,
        SyntaxGenerator generator)
    {
        StatementSyntax exitStatement = isParallelizationImpossible || !isResultQuery
            ? StatementEmitter.CreateContinue()
            : StatementEmitter.CreateReturn();

        return generator.IfStatement(
                generator.LogicalNotExpression(conditionExpression),
                [exitStatement])
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
    }
}