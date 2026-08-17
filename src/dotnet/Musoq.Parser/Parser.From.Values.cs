using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private bool IsValuesSource()
    {
        if (!IsContextualKeyword("values"))
            return false;

        var position = Current.Span.End;
        while (position < _lexer.Input.Length && char.IsWhiteSpace(_lexer.Input[position]))
            position++;

        return position < _lexer.Input.Length && _lexer.Input[position] == '{';
    }

    private ParsedSource ComposeValuesFrom()
    {
        var valuesToken = ConsumeAndGetToken(Current.TokenType);
        Consume(TokenType.LBracket);

        var rows = new List<ValuesRowNode>();
        while (Current.TokenType != TokenType.RBracket)
        {
            rows.Add(ComposeValuesRow());

            if (Current.TokenType != TokenType.Comma)
                break;

            Consume(TokenType.Comma);
            if (Current.TokenType == TokenType.RBracket)
                break;
        }

        var closingToken = ConsumeAndGetToken(TokenType.RBracket);
        var aliasResult = ComposeAlias(AliasContext.ValuesSource);
        EnsureAliasSyntax(aliasResult, AliasContext.ValuesSource);
        var alias = aliasResult.Alias;
        var aliasSpan = aliasResult.Span;

        if (string.IsNullOrWhiteSpace(alias))
            throw MissingRequiredAliasAfterDelimiter(
                "VALUES",
                "the closing brace",
                _lexer.AlreadyResolvedQueryPart,
                closingToken.Span);

        RegisterFromAlias(alias);

        var fromNode = new ValuesFromNode(rows, alias);
        fromNode.WithSpan(valuesToken.Span.Through(closingToken.Span));

        if (!aliasSpan.IsEmpty)
            fromNode.WithFullSpan(fromNode.Span.Through(aliasSpan));

        return ParsedSource.Create(fromNode, SourceKind.Values, valuesToken.Span.Start, closingToken.Span,
            aliasResult);
    }

    private ValuesRowNode ComposeValuesRow()
    {
        var openingToken = ConsumeAndGetToken(TokenType.LBracket);

        var fields = new List<ValuesFieldNode>();
        while (Current.TokenType != TokenType.RBracket)
        {
            var fieldToken = ComposeValuesFieldName();
            Consume(TokenType.Colon);

            fields.Add(new ValuesFieldNode(fieldToken.Value, ComposeOperations(), fieldToken.Span));

            if (Current.TokenType != TokenType.Comma)
                break;

            Consume(TokenType.Comma);
            if (Current.TokenType == TokenType.RBracket)
                break;
        }

        var closingToken = ConsumeAndGetToken(TokenType.RBracket);

        return new ValuesRowNode(fields, openingToken.Span.Through(closingToken.Span));
    }

    private Token ComposeValuesFieldName()
    {
        if (Current.TokenType is not (TokenType.Identifier or TokenType.Word))
            throw new SyntaxException(
                "Expected field name in VALUES row literal.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2002_MissingToken,
                Current.Span);

        return ConsumeAndGetToken(Current.TokenType);
    }
}
