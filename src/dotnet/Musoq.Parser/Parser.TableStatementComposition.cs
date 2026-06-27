using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private CreateTableNode ComposeTable()
    {
        var tableToken = ConsumeAndGetToken(Current.TokenType);
        var tableName = Current.Value;
        Consume(TokenType.Identifier);
        Consume(TokenType.LBracket);

        var columns = new List<CreateTableColumnDefinition>();
        while (Current.TokenType != TokenType.RBracket)
        {
            var fieldToken = ConsumeAndGetToken(TokenType.Identifier);
            var fieldName = fieldToken.Value;

            Consume(TokenType.Colon);

            var (typeName, columnEndSpan) = ComposeTableColumnTypeName();

            if (Current.TokenType == TokenType.QuestionMark)
            {
                typeName += "?";
                columnEndSpan = ConsumeAndGetToken(TokenType.QuestionMark).Span;
            }

            var readModifiers = ComposeTableColumnReadModifiers(fieldName);
            if (readModifiers.Count > 0)
                columnEndSpan = readModifiers[^1].Span;

            columns.Add(new CreateTableColumnDefinition(
                fieldName,
                typeName,
                readModifiers,
                fieldToken.Span.Through(columnEndSpan),
                fieldToken.Span));

            if (Current.TokenType == TokenType.Comma)
                Consume(TokenType.Comma);
        }

        var closingToken = ConsumeAndGetToken(Current.TokenType);

        return (CreateTableNode)new CreateTableNode(tableName, columns)
            .WithSpan(tableToken.Span.Through(closingToken.Span));
    }

    private (string TypeName, TextSpan Span) ComposeTableColumnTypeName()
    {
        var typeToken = ConsumeAndGetToken(TokenType.Identifier);
        var typeName = typeToken.Value;
        var typeEndSpan = typeToken.Span;

        while (Current.TokenType == TokenType.Dot)
        {
            Consume(TokenType.Dot);
            var segmentToken = ConsumeTableColumnTypeNameSegment();
            typeName += $".{segmentToken.Value}";
            typeEndSpan = segmentToken.Span;
        }

        return (typeName, typeEndSpan);
    }

    private Token ConsumeTableColumnTypeNameSegment()
    {
        if (Current.TokenType is TokenType.Identifier or TokenType.Property)
            return ConsumeAndGetToken(Current.TokenType);

        return ConsumeAndGetToken(TokenType.Identifier);
    }
}
