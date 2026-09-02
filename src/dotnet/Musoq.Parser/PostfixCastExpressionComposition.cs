using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private Node ComposePostfixExpression(int minPrecedence, Node? expression = null)
    {
        expression ??= ComposeBaseTypes(minPrecedence);

        while (Current.TokenType == TokenType.DoubleColon)
        {
            Consume(TokenType.DoubleColon);
            var targetTypeToken = ConsumeCastTypeName();
            var targetTypeName = targetTypeToken.Value;
            var targetTypeSpan = targetTypeToken.Span;
            if (Current.TokenType == TokenType.QuestionMark)
            {
                targetTypeName += Current.Value;
                targetTypeSpan = targetTypeSpan.Through(Current.Span);
                Consume(TokenType.QuestionMark);
            }

            var castNode = new CastNode(expression, targetTypeName);

            if (expression.HasSpan && !targetTypeSpan.IsEmpty)
                castNode.WithSpan(expression.Span.Through(targetTypeSpan));

            expression = castNode;
        }

        return expression;
    }
}
