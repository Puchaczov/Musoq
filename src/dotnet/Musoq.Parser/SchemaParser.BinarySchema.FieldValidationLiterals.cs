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
        lexer.Next();

        var values = new List<Node>();
        while (lexer.Current().TokenType != TokenType.EndOfFile)
        {
            values.Add(ToLiteralNode(lexer.Current(), fieldName, span));
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

    private Node ToLiteralNode(Token token, string fieldName, TextSpan span)
    {
        return token.TokenType switch
        {
            TokenType.Integer => new IntegerNode(token.Value, "i"),
            TokenType.HexadecimalInteger => new HexIntegerNode(token.Value),
            TokenType.BinaryInteger => new BinaryIntegerNode(token.Value),
            TokenType.OctalInteger => new OctalIntegerNode(token.Value),
            TokenType.Decimal => new DecimalNode(token.Value),
            TokenType.StringLiteral or TokenType.Word => new WordNode(token.Value),
            _ => throw InvalidFieldValueValidation(
                $"Field '{fieldName}' value validation expects a literal value but found '{token.TokenType}'.",
                span)
        };
    }

    private Node ComposeValidationScalar(string fieldName)
    {
        if (Current.TokenType == TokenType.Hyphen)
        {
            Consume(TokenType.Hyphen);
            return new HyphenNode(new IntegerNode("0", "i"), ComposeValidationScalar(fieldName));
        }

        var token = Current;
        var node = ToLiteralNode(token, fieldName, token.Span);
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
        if (element is ConstantValueNode constant
            && ToIntegralValue(constant.ObjValue) is >= 0 and <= 255)
            return;

        throw InvalidFieldValueValidation(
            $"Field '{fieldName}' byte value must be an integer between 0 and 255.",
            element.Span);
    }

    private static long? ToIntegralValue(object objValue)
    {
        return objValue switch
        {
            byte b => b,
            sbyte sb => sb,
            short s => s,
            ushort us => us,
            int i => i,
            uint ui => ui,
            long l => l,
            ulong ul when ul <= long.MaxValue => (long)ul,
            _ => null
        };
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
