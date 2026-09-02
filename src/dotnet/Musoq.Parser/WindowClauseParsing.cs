using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private FieldNode[] ComposeWindowPartitionFields()
    {
        var fields = new List<FieldNode>();
        var i = 0;

        do
        {
            var fieldExpression = ComposeOperations();
            fields.Add(new FieldNode(fieldExpression, i++, string.Empty));

            if (Current.TokenType != TokenType.Comma)
                break;

            Consume(TokenType.Comma);
            if (Current.TokenType == TokenType.RightParenthesis ||
                Current.TokenType == TokenType.OrderBy ||
                IsContextualKeyword("rows") ||
                IsContextualKeyword("range"))
                throw new SyntaxException(
                    "Window PARTITION BY list has a trailing comma. Add another expression or remove the comma.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2014_TrailingComma,
                    Current.Span);
        } while (true);

        return fields.ToArray();
    }

    private FieldOrderedNode[] ComposeWindowOrderedFields()
    {
        var fields = new List<FieldOrderedNode>();
        var i = 0;

        do
        {
            fields.Add(ConsumeFieldOrdered(i++));

            if (Current.TokenType != TokenType.Comma)
                break;

            Consume(TokenType.Comma);
            if (Current.TokenType == TokenType.RightParenthesis ||
                IsContextualKeyword("rows") ||
                IsContextualKeyword("range"))
                throw new SyntaxException(
                    "Window ORDER BY list has a trailing comma. Add another expression or remove the comma.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2014_TrailingComma,
                    Current.Span);
        } while (true);

        return fields.ToArray();
    }
}
