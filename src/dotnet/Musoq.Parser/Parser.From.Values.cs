﻿using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private bool IsValuesSource()
    {
        return IsContextualConstructStart("values", TokenType.LBracket);
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
        var openingToken = ConsumeAndGetToken(TokenType.LeftParenthesis);
        var (parsedFields, closingToken) = ComposeNamedFields(true, "VALUES row");
        var fields = new List<ValuesFieldNode>(parsedFields.Count);
        foreach (var field in parsedFields)
            fields.Add(new ValuesFieldNode(field.Name, field.Expression, field.NameSpan));

        return new ValuesRowNode(fields, openingToken.Span.Through(closingToken.Span));
    }
}