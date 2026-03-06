using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
///     Emitter for comparison operations, handling special cases like char vs string comparisons.
/// </summary>
public static class ComparisonEmitter
{
    /// <summary>
    ///     Processes an equality comparison, handling special char vs string cases.
    /// </summary>
    public static void ProcessEqualityComparison(
        Node leftNode,
        Node rightNode,
        Stack<SyntaxNode> nodes,
        SyntaxGenerator generator)
    {
        var b = nodes.Pop();
        var a = nodes.Pop();

        if (IsCharVsStringComparison(leftNode, rightNode))
        {
            var convertedComparison = HandleCharStringComparison(leftNode, rightNode, a, b, generator);
            nodes.Push(convertedComparison);
        }
        else
        {
            nodes.Push(a);
            nodes.Push(b);
            SyntaxBinaryOperationHelper.ProcessValueEqualsOperation(nodes, generator);
        }
    }

    /// <summary>
    ///     Processes a relational comparison (greater than, less than, etc.),
    ///     generating string.Compare for string operands instead of raw operators.
    /// </summary>
    public static void ProcessRelationalComparison(
        Node leftNode,
        Node rightNode,
        Stack<SyntaxNode> nodes,
        SyntaxGenerator generator,
        SyntaxKind comparisonKind,
        Action<Stack<SyntaxNode>, SyntaxGenerator> fallback)
    {
        if (IsStringVsStringComparison(leftNode, rightNode))
        {
            var right = nodes.Pop();
            var left = nodes.Pop();

            var stringCompareCall = CreateStringCompareCall(left, right);
            var zeroLiteral = SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(0));

            nodes.Push(SyntaxFactory.BinaryExpression(comparisonKind, stringCompareCall, zeroLiteral));
        }
        else
        {
            fallback(nodes, generator);
        }
    }

    private static bool IsStringVsStringComparison(Node leftNode, Node rightNode)
    {
        return leftNode.ReturnType == typeof(string) && rightNode.ReturnType == typeof(string);
    }

    private static InvocationExpressionSyntax CreateStringCompareCall(SyntaxNode left, SyntaxNode right)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                    SyntaxFactory.IdentifierName("Compare")))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument((ExpressionSyntax)left),
                        SyntaxFactory.Argument((ExpressionSyntax)right),
                        SyntaxFactory.Argument(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName("StringComparison"),
                                SyntaxFactory.IdentifierName("Ordinal")))
                    })));
    }

    private static bool IsCharVsStringComparison(Node leftNode, Node rightNode)
    {
        var leftIsChar = IsCharacterAccess(leftNode);
        var rightIsChar = IsCharacterAccess(rightNode);
        var leftIsString = leftNode is WordNode;
        var rightIsString = rightNode is WordNode;

        return (leftIsChar && rightIsString) || (leftIsString && rightIsChar);
    }

    private static bool IsCharacterAccess(Node node)
    {
        if (node is AccessObjectArrayNode arrayNode)
            return arrayNode.IsColumnAccess && arrayNode.ColumnType == typeof(string);
        return false;
    }

    private static SyntaxNode HandleCharStringComparison(
        Node leftNode,
        Node rightNode,
        SyntaxNode leftSyntax,
        SyntaxNode rightSyntax,
        SyntaxGenerator generator)
    {
        var leftIsChar = IsCharacterAccess(leftNode);

        if (leftIsChar && rightNode is WordNode rightWord)
        {
            var charValue = rightWord.Value.Length > 0 ? rightWord.Value[0] : '\0';
            var charLiteral = SyntaxFactory.LiteralExpression(
                SyntaxKind.CharacterLiteralExpression,
                SyntaxFactory.Literal(charValue));
            return generator.ValueEqualsExpression(leftSyntax, charLiteral);
        }

        if (leftNode is WordNode leftWordNode && IsCharacterAccess(rightNode))
        {
            var charValue = leftWordNode.Value.Length > 0 ? leftWordNode.Value[0] : '\0';
            var charLiteral = SyntaxFactory.LiteralExpression(
                SyntaxKind.CharacterLiteralExpression,
                SyntaxFactory.Literal(charValue));
            return generator.ValueEqualsExpression(charLiteral, rightSyntax);
        }

        return generator.ValueEqualsExpression(leftSyntax, rightSyntax);
    }
}
