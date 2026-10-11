using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private Node ComposeParameterIndexAccess(Node expression)
    {
        while (Current.TokenType == TokenType.LeftSquareBracket)
        {
            Consume(TokenType.LeftSquareBracket);
            var index = ComposeOperations();
            var closing = ConsumeAndGetToken(TokenType.RightSquareBracket);
            expression = new ArrayIndexNode(expression, index)
                .WithSpan(expression.Span.Through(closing.Span));
        }
        return expression;
    }
}
