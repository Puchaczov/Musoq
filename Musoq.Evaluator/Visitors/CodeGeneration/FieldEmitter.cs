using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
///     Handles code generation for field-related nodes.
/// </summary>
public static class FieldEmitter
{
    /// <summary>
    ///     Processes a FieldNode with potential cast optimization.
    /// </summary>
    /// <param name="returnType">The return type of the field.</param>
    /// <param name="expression">The current expression from the stack.</param>
    /// <param name="generator">The Roslyn syntax generator.</param>
    /// <returns>Result containing the expression and required types.</returns>
    public static FieldNodeResult ProcessFieldNode(Type returnType, SyntaxNode expression, SyntaxGenerator generator)
    {
        var types = EvaluationHelper.GetNestedTypes(returnType);
        var typeIdentifier = TypeNameHelper.GetTypeIdentifier(returnType);

        if (expression is CastExpressionSyntax castExpression &&
            castExpression.Type.ToString() == typeIdentifier.ToString())
            return new FieldNodeResult
            {
                Expression = expression,
                RequiredTypes = types
            };

        return new FieldNodeResult
        {
            Expression = generator.CastExpression(typeIdentifier, expression),
            RequiredTypes = types
        };
    }

    /// <summary>
    ///     Processes a FieldOrderedNode (always casts).
    /// </summary>
    /// <param name="returnType">The return type of the field.</param>
    /// <param name="expression">The current expression from the stack.</param>
    /// <param name="generator">The Roslyn syntax generator.</param>
    /// <returns>Result containing the cast expression and required types.</returns>
    public static FieldNodeResult ProcessFieldOrderedNode(Type returnType, SyntaxNode expression,
        SyntaxGenerator generator)
    {
        var types = EvaluationHelper.GetNestedTypes(returnType);
        var typeIdentifier = TypeNameHelper.GetTypeIdentifier(returnType);

        return new FieldNodeResult
        {
            Expression = generator.CastExpression(typeIdentifier, expression),
            RequiredTypes = types
        };
    }

    /// <summary>
    ///     Creates a member access expression for property value access.
    /// </summary>
    /// <param name="expression">The expression to access the property on.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>A member access expression.</returns>
    public static MemberAccessExpressionSyntax CreatePropertyAccess(ExpressionSyntax expression, string propertyName)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.ParenthesizedExpression(expression),
            SyntaxFactory.IdentifierName(propertyName));
    }

    /// <summary>
    ///     Result of processing a field node.
    /// </summary>
    public readonly struct FieldNodeResult
    {
        /// <summary>
        ///     The resulting expression (either original or cast).
        /// </summary>
        public SyntaxNode Expression { get; init; }

        /// <summary>
        ///     Types that need reference tracking.
        /// </summary>
        public Type[] RequiredTypes { get; init; }
    }
}
