using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
///     Helper class for handling binary operations during C# syntax generation.
///     Provides common implementation for operations that follow the pattern:
///     pop right, pop left, push new syntax expression.
/// </summary>
public static class SyntaxBinaryOperationHelper
{
    /// <summary>
    ///     Processes a binary operation by popping two syntax nodes and pushing the result.
    /// </summary>
    /// <param name="nodes">The syntax node stack.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <param name="expressionFactory">Factory to create the resulting expression from left and right operands.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes or generator is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    public static void ProcessBinaryOperation(
        Stack<SyntaxNode> nodes,
        SyntaxGenerator generator,
        Func<SyntaxNode, SyntaxNode, SyntaxNode> expressionFactory)
    {
        ValidateBinaryOperation(nodes, generator);
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(expressionFactory(left, right));
    }

    /// <summary>
    ///     Processes a unary operation by popping one syntax node and pushing the result.
    /// </summary>
    /// <param name="nodes">The syntax node stack.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <param name="expressionFactory">Factory to create the resulting expression from the operand.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes or generator is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    public static void ProcessUnaryOperation(
        Stack<SyntaxNode> nodes,
        SyntaxGenerator generator,
        Func<SyntaxNode, SyntaxNode> expressionFactory)
    {
        ValidateUnaryOperation(nodes, generator);
        var operand = nodes.Pop();
        nodes.Push(expressionFactory(operand));
    }

    /// <summary>Processes a multiplication operation.</summary>
    public static void ProcessMultiplyOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator) =>
        ProcessBinaryOperation(nodes, generator, (l, r) => generator.MultiplyExpression(l, r));

    /// <summary>Processes a division operation.</summary>
    public static void ProcessDivideOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator) =>
        ProcessBinaryOperation(nodes, generator, (l, r) => generator.DivideExpression(l, r));

    /// <summary>Processes a modulo operation.</summary>
    public static void ProcessModuloOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator) =>
        ProcessBinaryOperation(nodes, generator, (l, r) => generator.ModuloExpression(l, r));

    /// <summary>Processes an addition operation.</summary>
    public static void ProcessAddOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator) =>
        ProcessBinaryOperation(nodes, generator, (l, r) => generator.AddExpression(l, r));

    /// <summary>Processes a subtraction operation.</summary>
    public static void ProcessSubtractOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator) =>
        ProcessBinaryOperation(nodes, generator, (l, r) => generator.SubtractExpression(l, r));

    /// <summary>Processes a logical AND operation.</summary>
    public static void ProcessLogicalAndOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator) =>
        ProcessBinaryOperation(nodes, generator, (l, r) => generator.LogicalAndExpression(l, r));

    /// <summary>Processes a logical OR operation.</summary>
    public static void ProcessLogicalOrOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator) =>
        ProcessBinaryOperation(nodes, generator, (l, r) => generator.LogicalOrExpression(l, r));

    /// <summary>Processes an equality comparison operation.</summary>
    public static void ProcessValueEqualsOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator) =>
        ProcessBinaryOperation(nodes, generator, (l, r) => generator.ValueEqualsExpression(l, r));

    /// <summary>Processes a greater than or equal comparison operation.</summary>
    public static void ProcessGreaterThanOrEqualOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator) =>
        ProcessBinaryOperation(nodes, generator, (l, r) => generator.GreaterThanOrEqualExpression(l, r));

    /// <summary>Processes a less than or equal comparison operation.</summary>
    public static void ProcessLessThanOrEqualOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator) =>
        ProcessBinaryOperation(nodes, generator, (l, r) => generator.LessThanOrEqualExpression(l, r));

    /// <summary>Processes a greater than comparison operation.</summary>
    public static void ProcessGreaterThanOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator) =>
        ProcessBinaryOperation(nodes, generator, (l, r) => generator.GreaterThanExpression(l, r));

    /// <summary>Processes a less than comparison operation.</summary>
    public static void ProcessLessThanOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator) =>
        ProcessBinaryOperation(nodes, generator, (l, r) => generator.LessThanExpression(l, r));

    /// <summary>Processes a not equals comparison operation.</summary>
    public static void ProcessValueNotEqualsOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator) =>
        ProcessBinaryOperation(nodes, generator, (l, r) => generator.ValueNotEqualsExpression(l, r));

    /// <summary>Processes a logical NOT operation.</summary>
    public static void ProcessLogicalNotOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator) =>
        ProcessUnaryOperation(nodes, generator, n => generator.LogicalNotExpression(n));

    /// <summary>Processes a bitwise AND operation.</summary>
    public static void ProcessBitwiseAndOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator) =>
        ProcessBinaryOperation(nodes, generator, (l, r) => generator.BitwiseAndExpression(l, r));

    /// <summary>Processes a bitwise OR operation.</summary>
    public static void ProcessBitwiseOrOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator) =>
        ProcessBinaryOperation(nodes, generator, (l, r) => generator.BitwiseOrExpression(l, r));

    /// <summary>Processes a bitwise XOR operation.</summary>
    public static void ProcessBitwiseXorOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator) =>
        ProcessBinaryOperation(nodes, generator, (left, right) =>
            SyntaxFactory.BinaryExpression(SyntaxKind.ExclusiveOrExpression, (ExpressionSyntax)left, (ExpressionSyntax)right));

    /// <summary>Processes a left shift operation.</summary>
    public static void ProcessLeftShiftOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator) =>
        ProcessBinaryOperation(nodes, generator, (left, right) =>
            SyntaxFactory.BinaryExpression(SyntaxKind.LeftShiftExpression, (ExpressionSyntax)left, (ExpressionSyntax)right));

    /// <summary>Processes a right shift operation.</summary>
    public static void ProcessRightShiftOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator) =>
        ProcessBinaryOperation(nodes, generator, (left, right) =>
            SyntaxFactory.BinaryExpression(SyntaxKind.RightShiftExpression, (ExpressionSyntax)left, (ExpressionSyntax)right));

    private static void ValidateBinaryOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));

        if (generator == null)
            throw new ArgumentNullException(nameof(generator));

        if (nodes.Count < 2)
            throw new InvalidOperationException("Stack must contain at least 2 nodes for binary operation");
    }

    private static void ValidateUnaryOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));

        if (generator == null)
            throw new ArgumentNullException(nameof(generator));

        if (nodes.Count < 1)
            throw new InvalidOperationException("Stack must contain at least 1 node for unary operation");
    }
}
