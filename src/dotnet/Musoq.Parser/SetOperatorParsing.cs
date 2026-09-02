using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private (string[] Keys, TextSpan[] Spans) ComposeSetOperatorKeys()
    {
        var keys = new List<string>();
        var spans = new List<TextSpan>();

        if (Current.TokenType != TokenType.LeftParenthesis)
            return (keys.ToArray(), spans.ToArray());

        Consume(TokenType.LeftParenthesis);

        if (Current.TokenType == TokenType.RightParenthesis)
        {
            Consume(TokenType.RightParenthesis);
            return (keys.ToArray(), spans.ToArray());
        }

        if (Current.TokenType == TokenType.Comma)
            throw new SyntaxException(
                "Set operator key list has a leading comma. Add a key before the comma or remove it.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2015_LeadingComma,
                Current.Span);

        var firstKey = ParsePotentiallyDottedName();
        keys.Add(firstKey.Value);
        spans.Add(firstKey.Span);
        while (Current.TokenType == TokenType.Comma)
        {
            Consume(TokenType.Comma);
            if (Current.TokenType == TokenType.RightParenthesis)
                throw new SyntaxException(
                    "Set operator key list has a trailing comma. Add another key or remove the comma.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2014_TrailingComma,
                    Current.Span);

            var key = ParsePotentiallyDottedName();
            keys.Add(key.Value);
            spans.Add(key.Span);
        }

        Consume(TokenType.RightParenthesis);

        return (keys.ToArray(), spans.ToArray());
    }

    private (string Value, TextSpan Span) ParsePotentiallyDottedName()
    {
        var firstToken = ConsumeAndGetToken(Current.TokenType);
        var value = firstToken.Value;
        var span = firstToken.Span;
        if (Current.TokenType != TokenType.Dot)
            return (value, span);

        Consume(TokenType.Dot);
        var secondToken = ConsumeAndGetToken(Current.TokenType);
        value = $"{value}.{secondToken.Value}";
        return (value, span.Through(secondToken.Span));
    }
}
