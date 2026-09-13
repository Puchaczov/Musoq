﻿using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private readonly record struct ParsedNamedField(
        string Name,
        Node Expression,
        TextSpan NameSpan,
        TextSpan Span);

    private bool IsContextualArrayLiteralStart()
    {
        return IsContextualConstructStart("array", TokenType.LBracket);
    }

    private bool IsContextualConstructStart(string keyword, TokenType openingTokenType)
    {
        if (Current.TokenType is not (TokenType.Identifier or TokenType.Word or TokenType.Function) ||
            !Current.Value.Equals(keyword, StringComparison.OrdinalIgnoreCase))
            return false;

        return PeekSignificantToken().TokenType == openingTokenType;
    }

    private Token PeekSignificantToken(int significantDistance = 1)
    {
        if (significantDistance < 1)
            throw new ArgumentOutOfRangeException(nameof(significantDistance));

        var distance = 0;
        for (var lookahead = 1; ; lookahead++)
        {
            var token = _lexer.Peek(lookahead);
            if (token.TokenType is TokenType.WhiteSpace or TokenType.Comment)
                continue;

            distance++;
            if (distance == significantDistance)
                return token;
        }
    }

    private ArrayLiteralNode ComposeArrayLiteral()
    {
        var keyword = ConsumeAndGetToken(Current.TokenType);
        Consume(TokenType.LBracket);
        var elements = new List<Node>();

        if (Current.TokenType != TokenType.RBracket)
        {
            while (true)
            {
                elements.Add(ComposeOperations());
                if (Current.TokenType != TokenType.Comma)
                    break;

                Consume(TokenType.Comma);
                if (Current.TokenType == TokenType.RBracket)
                    break;
            }
        }

        var closing = ConsumeAndGetToken(TokenType.RBracket);
        return new ArrayLiteralNode(elements, keyword.Span.Through(closing.Span));
    }

    private bool IsRecordLiteralStart()
    {
        return IsStructuralFieldNameToken(Current.TokenType) &&
               PeekSignificantToken().TokenType == TokenType.Colon;
    }

    private bool IsParenthesizedRecordLiteralStart()
    {
        if (Current.TokenType != TokenType.LeftParenthesis)
            return false;

        var field = PeekSignificantToken();
        return IsStructuralFieldNameToken(field.TokenType) &&
               PeekSignificantToken(2).TokenType == TokenType.Colon;
    }

    private static bool IsStructuralFieldNameToken(TokenType tokenType)
    {
        return tokenType is TokenType.Identifier or TokenType.Word or TokenType.Function ||
               IsSchemaKeywordToken(tokenType) ||
               tokenType is TokenType.Window or TokenType.Rows or TokenType.Range or TokenType.Over or
                   TokenType.CurrentRow or TokenType.Following or TokenType.Preceding or TokenType.Unbounded;
    }

    private (List<ParsedNamedField> Fields, Token Closing) ComposeNamedFields(
        bool allowEmpty,
        string constructName)
    {
        var fields = new List<ParsedNamedField>();
        if (Current.TokenType == TokenType.RightParenthesis)
        {
            if (!allowEmpty)
                throw new SyntaxException(
                    $"A {constructName} must contain at least one named field.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2001_UnexpectedToken,
                    Current.Span);

            return (fields, ConsumeAndGetToken(TokenType.RightParenthesis));
        }

        while (true)
        {
            if (!IsStructuralFieldNameToken(Current.TokenType))
                throw new SyntaxException(
                    $"A {constructName} field must start with a field name.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2001_UnexpectedToken,
                    Current.Span);

            var nameToken = ConsumeAndGetToken(Current.TokenType);
            Consume(TokenType.Colon);
            var expression = ComposeOperations();
            fields.Add(new ParsedNamedField(
                nameToken.Value,
                expression,
                nameToken.Span,
                nameToken.Span.Through(expression.Span)));

            if (Current.TokenType != TokenType.Comma)
                break;

            Consume(TokenType.Comma);
            if (Current.TokenType == TokenType.RightParenthesis)
                break;
        }

        return (fields, ConsumeAndGetToken(TokenType.RightParenthesis));
    }

    private RecordLiteralNode ComposeRecordLiteral(Token opening)
    {
        var (parsedFields, closing) = ComposeNamedFields(false, "structural record");
        var fields = new List<RecordFieldNode>(parsedFields.Count);
        foreach (var field in parsedFields)
            fields.Add(new RecordFieldNode(field.Name, field.Expression, field.NameSpan, field.Span));

        return new RecordLiteralNode(fields, opening.Span.Through(closing.Span));
    }
}