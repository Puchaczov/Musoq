#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
/// Emitter for common statement patterns in code generation.
/// Handles if/else, loops, variable declarations, and other control flow statements.
/// </summary>
public class StatementEmitter
{
    /// <summary>
    /// Creates an if statement.
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
        
        if (elseStatement != null)
        {
            ifStatement = ifStatement.WithElse(SyntaxFactory.ElseClause(elseStatement));
        }
        
        return ifStatement;
    }

    /// <summary>
    /// Creates a return statement.
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
    /// Creates a continue statement.
    /// </summary>
    /// <returns>A continue statement syntax</returns>
    public static ContinueStatementSyntax CreateContinue()
    {
        return SyntaxFactory.ContinueStatement();
    }
    
    /// <summary>
    /// Creates a break statement.
    /// </summary>
    /// <returns>A break statement syntax</returns>
    public static BreakStatementSyntax CreateBreak()
    {
        return SyntaxFactory.BreakStatement();
    }
    
    /// <summary>
    /// Creates a throw statement.
    /// </summary>
    /// <param name="exceptionExpression">The exception to throw</param>
    /// <returns>A throw statement syntax</returns>
    public static ThrowStatementSyntax CreateThrow(ExpressionSyntax exceptionExpression)
    {
        return SyntaxFactory.ThrowStatement(exceptionExpression);
    }
    
    /// <summary>
    /// Creates an assignment statement by variable name.
    /// </summary>
    /// <param name="variableName">The variable name</param>
    /// <param name="value">The value to assign</param>
    /// <returns>An expression statement with assignment</returns>
    public static ExpressionStatementSyntax CreateAssignment(string variableName, ExpressionSyntax value)
    {
        return CreateAssignment(SyntaxFactory.IdentifierName(variableName), value);
    }
    
    /// <summary>
    /// Creates a for loop.
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
    /// Creates a foreach loop.
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
        return SyntaxFactory.ForEachStatement(
            SyntaxFactory.IdentifierName("var"),
            variableName,
            collectionExpression,
            body);
    }

    /// <summary>
    /// Creates a block from statements.
    /// </summary>
    /// <param name="statements">The statements</param>
    /// <returns>A block syntax</returns>
    public static BlockSyntax CreateBlock(params StatementSyntax[] statements)
    {
        return SyntaxFactory.Block(statements);
    }
    
    /// <summary>
    /// Creates a block from a collection of statements.
    /// </summary>
    /// <param name="statements">The statements collection</param>
    /// <returns>A block syntax</returns>
    public static BlockSyntax CreateBlock(IEnumerable<StatementSyntax> statements)
    {
        return SyntaxFactory.Block(statements);
    }
    
    /// <summary>
    /// Creates an empty block.
    /// </summary>
    /// <returns>An empty block syntax</returns>
    public static BlockSyntax CreateEmptyBlock()
    {
        return SyntaxFactory.Block();
    }

    /// <summary>
    /// Creates an argument list from expressions popped from a stack, reversing the order.
    /// Used for processing ArgsListNode where arguments are popped in reverse.
    /// </summary>
    /// <param name="nodes">The stack of syntax nodes.</param>
    /// <param name="count">The number of arguments to pop.</param>
    /// <returns>An argument list with arguments in correct order.</returns>
    public static ArgumentListSyntax CreateArgumentListFromStack(Stack<SyntaxNode> nodes, int count)
    {
        var args = SyntaxFactory.SeparatedList<ArgumentSyntax>();

        for (var i = 0; i < count; i++)
            args = args.Add(SyntaxFactory.Argument((ExpressionSyntax)nodes.Pop()));

        var rArgs = SyntaxFactory.SeparatedList<ArgumentSyntax>();
        for (var i = args.Count - 1; i >= 0; i--)
            rArgs = rArgs.Add(args[i]);

        return SyntaxFactory.ArgumentList(rArgs);
    }

    /// <summary>
    /// Creates an assignment statement.
    /// </summary>
    /// <param name="target">The assignment target</param>
    /// <param name="value">The value to assign</param>
    /// <returns>An expression statement with assignment</returns>
    private static ExpressionStatementSyntax CreateAssignment(
        ExpressionSyntax target, 
        ExpressionSyntax value)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                target,
                value));
    }
}
