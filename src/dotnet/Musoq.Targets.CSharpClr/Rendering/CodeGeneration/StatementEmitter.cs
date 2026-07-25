using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

/// <summary>
///     Emitter for common statement patterns in code generation.
///     Handles if/else, loops, variable declarations, and other control flow statements.
/// </summary>
public static class StatementEmitter
{
    /// <summary>
    ///     Creates an if statement.
    /// </summary>
    /// <param name="condition">The condition expression</param>
    /// <param name="thenStatement">The statement to execute when condition is true</param>
    /// <param name="elseStatement">Optional statement to execute when condition is false</param>
    /// <returns>An if statement syntax</returns>
    public static IfStatementSyntax CreateIf(
        ExpressionSyntax condition,
        StatementSyntax thenStatement,
        StatementSyntax? elseStatement = null)
    {
        var ifStatement = SyntaxFactory.IfStatement(condition, thenStatement);

        if (elseStatement != null) ifStatement = ifStatement.WithElse(SyntaxFactory.ElseClause(elseStatement));

        return ifStatement;
    }

    /// <summary>
    ///     Creates a return statement.
    /// </summary>
    /// <param name="expression">Optional expression to return</param>
    /// <returns>A return statement syntax</returns>
    public static ReturnStatementSyntax CreateReturn(ExpressionSyntax? expression = null)
    {
        return expression != null
            ? SyntaxFactory.ReturnStatement(expression)
            : SyntaxFactory.ReturnStatement();
    }

    /// <summary>
    ///     Creates a continue statement.
    /// </summary>
    /// <returns>A continue statement syntax</returns>
    public static ContinueStatementSyntax CreateContinue()
    {
        return SyntaxFactory.ContinueStatement();
    }

    /// <summary>
    ///     Creates a break statement.
    /// </summary>
    /// <returns>A break statement syntax</returns>
    public static BreakStatementSyntax CreateBreak()
    {
        return SyntaxFactory.BreakStatement();
    }

    /// <summary>
    ///     Creates a throw statement.
    /// </summary>
    /// <param name="exceptionExpression">The exception to throw</param>
    /// <returns>A throw statement syntax</returns>
    public static ThrowStatementSyntax CreateThrow(ExpressionSyntax exceptionExpression)
    {
        return SyntaxFactory.ThrowStatement(exceptionExpression);
    }

    /// <summary>
    ///     Creates a for loop.
    /// </summary>
    /// <param name="variableName">The loop variable name</param>
    /// <param name="startValue">The starting value</param>
    /// <param name="condition">The loop condition</param>
    /// <param name="incrementor">The incrementor expression</param>
    /// <param name="body">The loop body</param>
    /// <returns>A for statement syntax</returns>
    public static ForStatementSyntax CreateForLoop(
        string variableName,
        int startValue,
        ExpressionSyntax condition,
        ExpressionSyntax incrementor,
        StatementSyntax body)
    {
        return SyntaxFactory.ForStatement(body)
            .WithDeclaration(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(variableName)
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(startValue)))))))
            .WithCondition(condition)
            .WithIncrementors(SyntaxFactory.SingletonSeparatedList(incrementor));
    }

    /// <summary>
    ///     Creates a foreach loop.
    /// </summary>
    /// <param name="variableName">The loop variable name</param>
    /// <param name="collectionExpression">The collection to iterate over</param>
    /// <param name="body">The loop body</param>
    /// <returns>A foreach statement syntax</returns>
    public static ForEachStatementSyntax CreateForeach(
        string variableName,
        ExpressionSyntax collectionExpression,
        StatementSyntax body)
    {
        var blockBody = body is BlockSyntax block ? block : SyntaxFactory.Block(body);

        return SyntaxFactory.ForEachStatement(
            SyntaxFactory.IdentifierName("var"),
            variableName,
            collectionExpression,
            blockBody);
    }

    /// <summary>
    ///     Creates a block from statements.
    /// </summary>
    /// <param name="statements">The statements</param>
    /// <returns>A block syntax</returns>
    public static BlockSyntax CreateBlock(params StatementSyntax[] statements)
    {
        return SyntaxFactory.Block(statements);
    }

    /// <summary>
    ///     Creates a block from a collection of statements.
    /// </summary>
    /// <param name="statements">The statements collection</param>
    /// <returns>A block syntax</returns>
    public static BlockSyntax CreateBlock(IEnumerable<StatementSyntax> statements)
    {
        return SyntaxFactory.Block(statements);
    }

    /// <summary>
    ///     Creates an empty block.
    /// </summary>
    /// <returns>An empty block syntax</returns>
    public static BlockSyntax CreateEmptyBlock()
    {
        return SyntaxFactory.Block();
    }

}
