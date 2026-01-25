using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Schema.Helpers;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
///     Handles code generation for null check operations (IS NULL, IS NOT NULL).
/// </summary>
public static class NullCheckEmitter
{
    /// <summary>
    ///     Processes an IS NULL or IS NOT NULL check.
    /// </summary>
    /// <param name="returnType">The return type of the expression being checked.</param>
    /// <param name="isNegated">True for IS NOT NULL, false for IS NULL.</param>
    /// <param name="expression">The expression to check (only used if not a value type).</param>
    /// <returns>Result containing the null check expression.</returns>
    public static IsNullResult ProcessIsNull(Type returnType, bool isNegated, ExpressionSyntax expression)
    {
        if (returnType.IsTrueValueType())
            return new IsNullResult
            {
                Expression = isNegated
                    ? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                    : SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression),
                ShouldPopExpression = true
            };

        var comparisonKind = isNegated
            ? SyntaxKind.NotEqualsExpression
            : SyntaxKind.EqualsExpression;

        return new IsNullResult
        {
            Expression = SyntaxFactory.BinaryExpression(
                comparisonKind,
                expression,
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
            ShouldPopExpression = false
        };
    }

    /// <summary>
    ///     Result of processing an IsNullNode.
    /// </summary>
    public readonly struct IsNullResult
    {
        /// <summary>
        ///     The resulting expression.
        /// </summary>
        public SyntaxNode Expression { get; init; }

        /// <summary>
        ///     Whether the original expression should be consumed from the stack.
        /// </summary>
        public bool ShouldPopExpression { get; init; }
    }
}
