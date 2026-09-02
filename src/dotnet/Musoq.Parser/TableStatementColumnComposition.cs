using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private List<CreateTableColumnDefinition> ComposeTableColumns()
    {
        var columns = new List<CreateTableColumnDefinition>();
        while (Current.TokenType != TokenType.RBracket)
        {
            if (Current.TokenType == TokenType.EndOfFile)
                ThrowUnexpectedTableEnd();

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
            else if (Current.TokenType == TokenType.EndOfFile)
                ThrowUnexpectedTableEnd();
        }

        return columns;
    }

    private void ThrowUnexpectedTableEnd()
    {
        throw new SyntaxException(
            "The TABLE definition ends unexpectedly; expected a closing '}'.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2017_UnexpectedEndOfFile,
            new TextSpan(Current.Span.Start, 0));
    }

}
