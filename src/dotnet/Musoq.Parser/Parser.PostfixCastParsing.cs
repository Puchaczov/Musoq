using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private Node ComposePostfixExpression(int minPrecedence)
    {
        var expression = ComposeBaseTypes(minPrecedence);

        while (Current.TokenType == TokenType.DoubleColon)
        {
            Consume(TokenType.DoubleColon);
            var targetTypeToken = ConsumeCastTypeName();
            var castNode = new CastNode(expression, targetTypeToken.Value);

            if (expression.HasSpan && !targetTypeToken.Span.IsEmpty)
                castNode.WithSpan(expression.Span.Through(targetTypeToken.Span));

            expression = castNode;
        }

        return expression;
    }

    private Token ConsumeCastTypeName()
    {
        if (Current.TokenType == TokenType.Identifier)
            return ConsumeAndGetToken(TokenType.Identifier);

        throw new SyntaxException(
            "Expected cast target type name after '::'.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2001_UnexpectedToken,
            Current.Span);
    }
}
