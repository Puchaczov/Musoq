using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Helper class for handling binary operations during C# syntax generation.
/// Provides common implementation for operations that follow the pattern:
/// pop right, pop left, push new syntax expression.
/// </summary>
public static class SyntaxBinaryOperationHelper
{
    /// <summary>
    /// Processes a multiplication operation.
    /// </summary>
    /// <param name="nodes">The syntax node stack.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes or generator is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    public static void ProcessMultiplyOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator)
    {
        ValidateBinaryOperation(nodes, generator);
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(generator.MultiplyExpression(left, right));
    }

    /// <summary>
    /// Processes a division operation.
    /// </summary>
    /// <param name="nodes">The syntax node stack.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes or generator is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    public static void ProcessDivideOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator)
    {
        ValidateBinaryOperation(nodes, generator);
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(generator.DivideExpression(left, right));
    }

    /// <summary>
    /// Processes a modulo operation.
    /// </summary>
    /// <param name="nodes">The syntax node stack.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes or generator is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    public static void ProcessModuloOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator)
    {
        ValidateBinaryOperation(nodes, generator);
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(generator.ModuloExpression(left, right));
    }

    /// <summary>
    /// Processes an addition operation.
    /// </summary>
    /// <param name="nodes">The syntax node stack.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes or generator is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    public static void ProcessAddOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator)
    {
        ValidateBinaryOperation(nodes, generator);
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(generator.AddExpression(left, right));
    }

    /// <summary>
    /// Processes a subtraction operation.
    /// </summary>
    /// <param name="nodes">The syntax node stack.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes or generator is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    public static void ProcessSubtractOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator)
    {
        ValidateBinaryOperation(nodes, generator);
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(generator.SubtractExpression(left, right));
    }

    /// <summary>
    /// Processes a logical AND operation.
    /// </summary>
    /// <param name="nodes">The syntax node stack.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes or generator is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    public static void ProcessLogicalAndOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator)
    {
        ValidateBinaryOperation(nodes, generator);
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(generator.LogicalAndExpression(left, right));
    }

    /// <summary>
    /// Processes a logical OR operation.
    /// </summary>
    /// <param name="nodes">The syntax node stack.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes or generator is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    public static void ProcessLogicalOrOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator)
    {
        ValidateBinaryOperation(nodes, generator);
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(generator.LogicalOrExpression(left, right));
    }

    /// <summary>
    /// Processes an equality comparison operation.
    /// </summary>
    /// <param name="nodes">The syntax node stack.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes or generator is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    public static void ProcessValueEqualsOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator)
    {
        ValidateBinaryOperation(nodes, generator);
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(generator.ValueEqualsExpression(left, right));
    }

    /// <summary>
    /// Processes a greater than or equal comparison operation.
    /// </summary>
    /// <param name="nodes">The syntax node stack.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes or generator is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    public static void ProcessGreaterThanOrEqualOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator)
    {
        ValidateBinaryOperation(nodes, generator);
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(generator.GreaterThanOrEqualExpression(left, right));
    }

    /// <summary>
    /// Processes a less than or equal comparison operation.
    /// </summary>
    /// <param name="nodes">The syntax node stack.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes or generator is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    public static void ProcessLessThanOrEqualOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator)
    {
        ValidateBinaryOperation(nodes, generator);
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(generator.LessThanOrEqualExpression(left, right));
    }

    /// <summary>
    /// Processes a greater than comparison operation.
    /// </summary>
    /// <param name="nodes">The syntax node stack.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes or generator is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    public static void ProcessGreaterThanOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator)
    {
        ValidateBinaryOperation(nodes, generator);
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(generator.GreaterThanExpression(left, right));
    }

    /// <summary>
    /// Processes a less than comparison operation.
    /// </summary>
    /// <param name="nodes">The syntax node stack.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes or generator is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    public static void ProcessLessThanOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator)
    {
        ValidateBinaryOperation(nodes, generator);
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(generator.LessThanExpression(left, right));
    }

    /// <summary>
    /// Processes a not equals comparison operation.
    /// </summary>
    /// <param name="nodes">The syntax node stack.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes or generator is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    public static void ProcessValueNotEqualsOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator)
    {
        ValidateBinaryOperation(nodes, generator);
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(generator.ValueNotEqualsExpression(left, right));
    }

    /// <summary>
    /// Validates that the stack and generator are not null and stack has at least 2 nodes for binary operations.
    /// </summary>
    /// <param name="nodes">The syntax node stack to validate.</param>
    /// <param name="generator">The syntax generator to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes or generator is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    private static void ValidateBinaryOperation(Stack<SyntaxNode> nodes, SyntaxGenerator generator)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));
            
        if (generator == null)
            throw new ArgumentNullException(nameof(generator));
            
        if (nodes.Count < 2)
            throw new InvalidOperationException("Stack must contain at least 2 nodes for binary operation");
    }
}