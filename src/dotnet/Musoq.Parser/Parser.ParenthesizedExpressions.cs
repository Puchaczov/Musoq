using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private Node ComposeParenthesizedExpressionOrScalarSubquery()
    {
        using var sourceRegion = _sourceCapture?.Begin(Syntax.SyntaxKind.ParenthesizedExpression, Current);
        var opening = ConsumeAndGetToken(TokenType.LeftParenthesis);

        if (IsRecordLiteralStart())
            return ComposeRecordLiteral(opening);

        if (Current.TokenType == TokenType.Select || Current.TokenType == TokenType.From || Current.TokenType == TokenType.Pivot || Current.TokenType == TokenType.Unpivot)
        {
            var subquery = ComposeSetOperators(1);
            Consume(TokenType.RightParenthesis);
            return new ScalarSubqueryNode(subquery);
        }

        var expression = ComposeOperations();
        ConsumeClosingParenthesis(
            DiagnosticCode.MQ2010_MissingClosingParenthesis,
            "A grouped expression is missing its closing parenthesis.");
        return expression;
    }

    private Token ConsumeClosingParenthesis(DiagnosticCode code, string message)
    {
        if (Current.TokenType != TokenType.RightParenthesis)
            throw new SyntaxException(
                message,
                _lexer.AlreadyResolvedQueryPart,
                code,
                new TextSpan(Current.Span.Start, 0));

        return ConsumeAndGetToken(TokenType.RightParenthesis);
    }

}
