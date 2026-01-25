using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
///     Handles code generation for pagination (SKIP/TAKE) clauses.
/// </summary>
public static class PaginationEmitter
{
    private const string SkipIdentifier = "skipAmount";
    private const string TakeIdentifier = "tookAmount";

    /// <summary>
    ///     Generates code for SKIP clause.
    /// </summary>
    /// <param name="skipValue">The number of rows to skip.</param>
    /// <param name="generator">The Roslyn syntax generator.</param>
    /// <returns>Result containing declaration and if statement.</returns>
    public static SkipNodeResult GenerateSkipCode(long skipValue, SyntaxGenerator generator)
    {
        var skip = SyntaxFactory.LocalDeclarationStatement(
                SyntaxHelper.CreateAssignment(SkipIdentifier, (ExpressionSyntax)generator.LiteralExpression(1)))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        var ifStatement = generator.IfStatement(
            generator.LessThanOrEqualExpression(
                SyntaxFactory.IdentifierName(SkipIdentifier),
                generator.LiteralExpression(skipValue)),
            [
                SyntaxFactory.PostfixUnaryExpression(
                    SyntaxKind.PostIncrementExpression,
                    SyntaxFactory.IdentifierName(SkipIdentifier)),
                StatementEmitter.CreateContinue()
            ]);

        return new SkipNodeResult
        {
            Declaration = skip,
            IfStatement = ifStatement
        };
    }

    /// <summary>
    ///     Generates code for TAKE clause.
    /// </summary>
    /// <param name="takeValue">The maximum number of rows to take.</param>
    /// <param name="generator">The Roslyn syntax generator.</param>
    /// <returns>A block containing the if statement and increment logic.</returns>
    public static (LocalDeclarationStatementSyntax Declaration, BlockSyntax Block) GenerateTakeCode(
        long takeValue,
        SyntaxGenerator generator)
    {
        var take = SyntaxFactory.LocalDeclarationStatement(
            SyntaxHelper.CreateAssignment(TakeIdentifier, (ExpressionSyntax)generator.LiteralExpression(0)));

        var ifStatement = (StatementSyntax)generator.IfStatement(
            generator.ValueEqualsExpression(
                SyntaxFactory.IdentifierName(TakeIdentifier),
                generator.LiteralExpression(takeValue)),
            [
                StatementEmitter.CreateBreak()
            ]);

        var incTookAmount = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.PostfixUnaryExpression(
                SyntaxKind.PostIncrementExpression,
                SyntaxFactory.IdentifierName(TakeIdentifier)));

        return (take, StatementEmitter.CreateBlock(ifStatement, incTookAmount));
    }

    /// <summary>
    ///     Result of processing a Skip node.
    /// </summary>
    public readonly struct SkipNodeResult
    {
        /// <summary>
        ///     The local declaration statement for the skip counter.
        /// </summary>
        public LocalDeclarationStatementSyntax Declaration { get; init; }

        /// <summary>
        ///     The if statement to check skip condition.
        /// </summary>
        public SyntaxNode IfStatement { get; init; }
    }
}
