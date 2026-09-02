using System.Globalization;
using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private List<Node> ComposeByteList(string fieldName)
    {
        var values = ComposeBracketedLiterals(fieldName);
        foreach (var value in values)
            EnsureByteValue(value, fieldName);

        return values;
    }

    private List<Node> ComposeBracketedLiterals(string fieldName)
    {
        Consume(TokenType.LeftSquareBracket);

        if (Current.TokenType == TokenType.RightSquareBracket)
        {
            Consume(TokenType.RightSquareBracket);
            return [];
        }

        var contentToken = Current;
        Consume(contentToken.TokenType);
        Consume(TokenType.RightSquareBracket);

        return LexLiteralList(contentToken.Value, fieldName, contentToken.Span);
    }

    private List<Node> LexLiteralList(string content, string fieldName, TextSpan span)
    {
        var lexer = new Lexer(content, true);
        var contentOffset = span.Start + (content.Length - content.TrimStart().Length);
        lexer.Next();

        var values = new List<Node>();
        while (lexer.Current().TokenType != TokenType.EndOfFile)
        {
            var token = lexer.Current();
            Node value;
            if (token.TokenType == TokenType.Hyphen)
            {
                var signSpan = TranslateLiteralSpan(token.Span, contentOffset);
                lexer.Next();
                if (!IsLiteralToken(lexer.Current()))
                    throw InvalidFieldValueValidation(
                        $"Field '{fieldName}' value validation expects a literal after '-'.",
                        signSpan);

                var operand = ToLiteralNode(lexer.Current(), fieldName, contentOffset);
                value = new HyphenNode(new IntegerNode(0, signSpan), operand)
                    .WithSpan(signSpan.Through(operand.Span));
            }
            else
            {
                value = ToLiteralNode(token, fieldName, contentOffset);
            }

            values.Add(value);
            lexer.Next();

            if (lexer.Current().TokenType != TokenType.Comma)
                break;

            lexer.Next();
        }

        if (lexer.Current().TokenType != TokenType.EndOfFile)
            throw InvalidFieldValueValidation(
                $"Field '{fieldName}' value validation expects a comma-separated list of literals.",
                span);

        return values;
    }

    private Node ToLiteralNode(Token token, string fieldName, int sourceOffset)
    {
        var span = TranslateLiteralSpan(token.Span, sourceOffset);
        return token.TokenType switch
        {
            TokenType.Integer => new IntegerNode(token.Value, token is IntegerToken { Abbreviation.Length: > 0 } integer
                ? integer.Abbreviation
                : "i", span),
            TokenType.HexadecimalInteger => new HexIntegerNode(token.Value, span),
            TokenType.BinaryInteger => new BinaryIntegerNode(token.Value, span),
            TokenType.OctalInteger => new OctalIntegerNode(token.Value, span),
            TokenType.Decimal => new DecimalNode(token.Value, span),
            TokenType.StringLiteral or TokenType.Word => new WordNode(token.Value, span),
            _ => throw InvalidFieldValueValidation(
                $"Field '{fieldName}' value validation expects a literal value but found '{token.TokenType}'.",
                span)
        };
    }

    private Node ComposeValidationScalar(string fieldName)
    {
        if (Current.TokenType == TokenType.Hyphen)
        {
            var minusToken = ConsumeAndGetToken(TokenType.Hyphen);
            var operand = ComposeValidationScalar(fieldName);
            return new HyphenNode(new IntegerNode(0, minusToken.Span), operand)
                .WithSpan(minusToken.Span.Through(operand.Span));
        }

        var token = Current;
        var node = ToLiteralNode(token, fieldName, 0);
        Consume(token.TokenType);
        return node;
    }

    private void EnsureScalarCompatible(TypeAnnotationNode typeAnnotation, string fieldName, TextSpan keywordSpan)
    {
        if (typeAnnotation is PrimitiveTypeNode or BitsTypeNode or StringTypeNode)
            return;

        throw InvalidFieldValueValidation(
            $"Field '{fieldName}' scalar value validation is only supported for numeric, bits, and string types.",
            keywordSpan);
    }

    private void EnsureByteListCompatible(TypeAnnotationNode typeAnnotation, string fieldName, TextSpan keywordSpan)
    {
        if (typeAnnotation is ByteArrayTypeNode)
            return;

        if (typeAnnotation is SubstreamTypeNode { Mode: SubstreamMode.Raw })
            return;

        throw InvalidFieldValueValidation(
            $"Field '{fieldName}' byte-list value validation is only supported for 'byte[n]' and raw substream types.",
            keywordSpan);
    }

    private void EnsureByteValue(Node element, string fieldName)
    {
        if (TryGetNumericValue(element, out var value) &&
            decimal.Truncate(value) == value && value is >= 0 and <= 255)
            return;

        throw InvalidFieldValueValidation(
            $"Field '{fieldName}' byte value must be an integer between 0 and 255.",
            element.Span);
    }

    private void EnsureScalarValueCompatible(
        TypeAnnotationNode typeAnnotation,
        Node value,
        string fieldName,
        TextSpan keywordSpan)
    {
        var valueSpan = value.HasSpan ? value.Span : keywordSpan;

        if (typeAnnotation is StringTypeNode)
        {
            if (value is WordNode)
                return;

            throw InvalidFieldValueValidation(
                $"Field '{fieldName}' string value validation requires a string literal.",
                valueSpan);
        }

        if (typeAnnotation is BitsTypeNode bitsType)
        {
            if (!TryGetBitValue(value, out var bitValue))
                throw InvalidFieldValueValidation(
                    $"Field '{fieldName}' bits value validation requires a non-negative integer literal.",
                    valueSpan);

            var maximum = bitsType.BitCount == 64
                ? ulong.MaxValue
                : (1UL << bitsType.BitCount) - 1;
            if (bitValue > maximum)
                throw InvalidFieldValueValidation(
                    $"Field '{fieldName}' bits value {bitValue} does not fit in {bitsType.BitCount} bits.",
                    valueSpan);

            return;
        }

        if (typeAnnotation is PrimitiveTypeNode && !IsNumericLiteral(value))
            throw InvalidFieldValueValidation(
                $"Field '{fieldName}' numeric value validation requires a numeric literal.",
                valueSpan);
    }

    private static bool IsLiteralToken(Token token)
    {
        return token.TokenType is TokenType.Integer or TokenType.HexadecimalInteger or
            TokenType.BinaryInteger or TokenType.OctalInteger or TokenType.Decimal or
            TokenType.StringLiteral or TokenType.Word;
    }

    private static TextSpan TranslateLiteralSpan(TextSpan span, int sourceOffset)
    {
        return span.IsEmpty ? span : new TextSpan(sourceOffset + span.Start, span.Length);
    }

    private static bool IsNumericLiteral(Node node)
    {
        if (node is HyphenNode hyphen)
            return IsNumericLiteral(hyphen.Left) && IsNumericLiteral(hyphen.Right);

        return node is ConstantValueNode constant && IsNumericObject(constant.ObjValue);
    }

    private static bool IsNumericObject(object value)
    {
        return value is byte or sbyte or short or ushort or int or uint or long or ulong or
            float or double or decimal;
    }

    private static bool TryGetNumericValue(Node node, out decimal value)
    {
        if (node is HyphenNode hyphen &&
            TryGetNumericValue(hyphen.Left, out var left) &&
            TryGetNumericValue(hyphen.Right, out var right))
        {
            value = left - right;
            return true;
        }

        if (node is ConstantValueNode constant && IsNumericObject(constant.ObjValue))
        {
            try
            {
                value = Convert.ToDecimal(constant.ObjValue, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception) when (constant.ObjValue is double or float)
            {
                // NaN and infinity are not valid integer/byte validation literals.
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetBitValue(Node node, out ulong value)
    {
        if ((node is HexIntegerNode or BinaryIntegerNode or OctalIntegerNode) &&
            node is ConstantValueNode { ObjValue: long signedValue } && signedValue < 0)
        {
            value = unchecked((ulong)signedValue);
            return true;
        }

        if (!TryGetNumericValue(node, out var numeric) ||
            numeric < 0 || numeric > ulong.MaxValue || decimal.Truncate(numeric) != numeric)
        {
            value = default;
            return false;
        }

        value = Convert.ToUInt64(numeric, CultureInfo.InvariantCulture);
        return true;
    }

    private SyntaxException InvalidFieldValueValidation(string message, TextSpan span)
    {
        return new SyntaxException(
            message,
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ4006_InvalidFieldConstraint,
            span);
    }
}
