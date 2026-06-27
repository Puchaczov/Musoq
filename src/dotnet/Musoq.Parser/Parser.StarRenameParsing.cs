using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private StarRenameItemNode[] ComposeRenameList()
    {
        if (Current.TokenType != TokenType.LeftParenthesis)
            throw new SyntaxException(
                $"RENAME requires a parenthesized list. Expected '(' but found '{Current.Value}'. Usage: RENAME (OldName AS NewName).",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span);

        Consume(TokenType.LeftParenthesis);

        if (Current.TokenType == TokenType.RightParenthesis)
            throw new SyntaxException(
                "RENAME list must contain at least one rename. Usage: RENAME (OldName AS NewName).",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2003_InvalidExpression,
                Current.Span);

        var items = new List<StarRenameItemNode>();

        do
        {
            if (Current.TokenType == TokenType.Comma)
                Consume(TokenType.Comma);

            var sourceName = ConsumePossiblyQualifiedColumnIdentifier();

            if (Current.TokenType != TokenType.As)
                throw new SyntaxException(
                    $"Expected AS keyword after source column in RENAME item but found '{Current.Value}'. Usage: RENAME (OldName AS NewName).",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2001_UnexpectedToken,
                    Current.Span);

            Consume(TokenType.As);

            items.Add(new StarRenameItemNode(sourceName, ConsumeColumnIdentifier()));
        } while (Current.TokenType == TokenType.Comma);

        if (Current.TokenType != TokenType.RightParenthesis)
            throw new SyntaxException(
                $"Expected ')' to close RENAME list but found '{Current.Value}'. Check for missing commas between items.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span);

        Consume(TokenType.RightParenthesis);

        return items.ToArray();
    }

    private string ConsumePossiblyQualifiedColumnIdentifier()
    {
        var columnName = ConsumeColumnIdentifier();

        if (Current.TokenType != TokenType.Dot)
            return columnName;

        Consume(TokenType.Dot);

        if (Current.TokenType == TokenType.Property)
        {
            var memberName = Current.Value;
            Consume(TokenType.Property);
            return $"{columnName}.{memberName}";
        }

        return $"{columnName}.{ConsumeColumnIdentifier()}";
    }
}
