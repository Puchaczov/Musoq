using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Helper class for converting literal node types to C# syntax.
/// Provides specialized conversion for different literal types.
/// </summary>
public static class LiteralNodeSyntaxConverter
{
    /// <summary>
    /// Converts a StringNode to C# string literal syntax.
    /// </summary>
    /// <param name="node">The string node to convert.</param>
    /// <returns>A C# string literal expression.</returns>
    /// <exception cref="ArgumentNullException">Thrown when node is null.</exception>
    public static LiteralExpressionSyntax ConvertStringNode(StringNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        var escaped = SymbolDisplay.FormatLiteral(node.Value, true);
        var token = SyntaxFactory.Literal(escaped, node.Value);
        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, token);
    }

    /// <summary>
    /// Converts a DecimalNode to C# decimal literal syntax.
    /// </summary>
    /// <param name="node">The decimal node to convert.</param>
    /// <returns>A C# decimal literal expression.</returns>
    /// <exception cref="ArgumentNullException">Thrown when node is null.</exception>
    public static CastExpressionSyntax ConvertDecimalNode(DecimalNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        return SyntaxFactory.CastExpression(
                SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(SyntaxKind.DecimalKeyword)),
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(node.Value)))
            .WithOpenParenToken(
                SyntaxFactory.Token(SyntaxKind.OpenParenToken))
            .WithCloseParenToken(
                SyntaxFactory.Token(SyntaxKind.CloseParenToken))
            .NormalizeWhitespace();
    }

    /// <summary>
    /// Converts an IntegerNode to C# integer literal syntax based on the node's return type.
    /// </summary>
    /// <param name="node">The integer node to convert.</param>
    /// <returns>A C# integer literal expression with appropriate casting.</returns>
    /// <exception cref="ArgumentNullException">Thrown when node is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the node's return type is not supported.</exception>
    public static CastExpressionSyntax ConvertIntegerNode(IntegerNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        return node.ReturnType switch
        {
            { } t when t == typeof(sbyte) => CreateIntegerCast(SyntaxKind.SByteKeyword, (sbyte)node.ObjValue),
            { } t when t == typeof(byte) => CreateIntegerCast(SyntaxKind.ByteKeyword, (byte)node.ObjValue),
            { } t when t == typeof(short) => CreateIntegerCast(SyntaxKind.ShortKeyword, (short)node.ObjValue),
            { } t when t == typeof(ushort) => CreateIntegerCast(SyntaxKind.UShortKeyword, (ushort)node.ObjValue),
            { } t when t == typeof(int) => CreateIntegerCast(SyntaxKind.IntKeyword, (int)node.ObjValue),
            { } t when t == typeof(uint) => CreateIntegerCast(SyntaxKind.UIntKeyword, (uint)node.ObjValue),
            { } t when t == typeof(long) => CreateIntegerCast(SyntaxKind.LongKeyword, (long)node.ObjValue),
            { } t when t == typeof(ulong) => CreateIntegerCast(SyntaxKind.ULongKeyword, (ulong)node.ObjValue),
            _ => throw new NotSupportedException($"Type {node.ReturnType} is not supported.")
        };
    }

    /// <summary>
    /// Converts a HexIntegerNode to C# integer literal syntax.
    /// </summary>
    /// <param name="node">The hex integer node to convert.</param>
    /// <returns>A C# long literal expression.</returns>
    /// <exception cref="ArgumentNullException">Thrown when node is null.</exception>
    public static CastExpressionSyntax ConvertHexIntegerNode(HexIntegerNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        return CreateIntegerCast(SyntaxKind.LongKeyword, (long)node.ObjValue);
    }

    /// <summary>
    /// Converts a BinaryIntegerNode to C# integer literal syntax.
    /// </summary>
    /// <param name="node">The binary integer node to convert.</param>
    /// <returns>A C# long literal expression.</returns>
    /// <exception cref="ArgumentNullException">Thrown when node is null.</exception>
    public static CastExpressionSyntax ConvertBinaryIntegerNode(BinaryIntegerNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        return CreateIntegerCast(SyntaxKind.LongKeyword, (long)node.ObjValue);
    }

    /// <summary>
    /// Converts a OctalIntegerNode to C# integer literal syntax.
    /// </summary>
    /// <param name="node">The octal integer node to convert.</param>
    /// <returns>A C# long literal expression.</returns>
    /// <exception cref="ArgumentNullException">Thrown when node is null.</exception>
    public static CastExpressionSyntax ConvertOctalIntegerNode(OctalIntegerNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        return CreateIntegerCast(SyntaxKind.LongKeyword, (long)node.ObjValue);
    }

    /// <summary>
    /// Converts a BooleanNode to C# boolean literal syntax.
    /// </summary>
    /// <param name="node">The boolean node to convert.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <returns>A C# boolean literal expression.</returns>
    /// <exception cref="ArgumentNullException">Thrown when node or generator is null.</exception>
    public static SyntaxNode ConvertBooleanNode(BooleanNode node, SyntaxGenerator generator)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (generator == null)
            throw new ArgumentNullException(nameof(generator));

        return generator.LiteralExpression(node.Value);
    }

    /// <summary>
    /// Converts a WordNode to C# string literal syntax.
    /// </summary>
    /// <param name="node">The word node to convert.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <returns>A C# string literal expression.</returns>
    /// <exception cref="ArgumentNullException">Thrown when node or generator is null.</exception>
    public static SyntaxNode ConvertWordNode(WordNode node, SyntaxGenerator generator)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (generator == null)
            throw new ArgumentNullException(nameof(generator));

        return generator.LiteralExpression(node.Value);
    }

    /// <summary>
    /// Converts a NullNode to C# null literal syntax with proper nullable handling.
    /// </summary>
    /// <param name="node">The null node to convert.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <returns>A C# null literal expression with proper nullable type casting.</returns>
    /// <exception cref="ArgumentNullException">Thrown when node or generator is null.</exception>
    public static SyntaxNode ConvertNullNode(NullNode node, SyntaxGenerator generator)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (generator == null)
            throw new ArgumentNullException(nameof(generator));

        if (CheckIfNullable(node.ReturnType))
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
        }

        var typeIdentifier = SyntaxFactory.IdentifierName(
            EvaluationHelper.GetCastableType(node.ReturnType));

        return generator.CastExpression(generator.NullableTypeExpression(typeIdentifier),
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
    }

    /// <summary>
    /// Creates a cast expression for integer types.
    /// </summary>
    /// <param name="keyword">The keyword for the target type.</param>
    /// <param name="value">The value to cast.</param>
    /// <returns>A cast expression.</returns>
    private static CastExpressionSyntax CreateIntegerCast(SyntaxKind keyword, object value)
    {
        var literal = keyword switch
        {
            SyntaxKind.SByteKeyword => SyntaxFactory.Literal((sbyte)value),
            SyntaxKind.ByteKeyword => SyntaxFactory.Literal((byte)value),
            SyntaxKind.ShortKeyword => SyntaxFactory.Literal((short)value),
            SyntaxKind.UShortKeyword => SyntaxFactory.Literal((ushort)value),
            SyntaxKind.IntKeyword => SyntaxFactory.Literal((int)value),
            SyntaxKind.UIntKeyword => SyntaxFactory.Literal((uint)value),
            SyntaxKind.LongKeyword => SyntaxFactory.Literal((long)value),
            SyntaxKind.ULongKeyword => SyntaxFactory.Literal((ulong)value),
            _ => throw new ArgumentException($"Unsupported integer type keyword: {keyword}")
        };

        return SyntaxFactory.CastExpression(
                SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(keyword)),
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    literal))
            .WithOpenParenToken(
                SyntaxFactory.Token(SyntaxKind.OpenParenToken))
            .WithCloseParenToken(
                SyntaxFactory.Token(SyntaxKind.CloseParenToken));
    }

    /// <summary>
    /// Checks if a type is nullable.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is nullable, false otherwise.</returns>
    private static bool CheckIfNullable(Type type)
    {
        if (type.IsValueType)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        return true;
    }
}