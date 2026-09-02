using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private GroupByNode ComposeGroupByAllNode()
    {
        var allToken = ConsumeAndGetToken(TokenType.All);
        if (Current.TokenType == TokenType.Comma)
            throw GroupByAllWithExplicitFields(Current.Span);
        if (Current.TokenType != TokenType.Having)
            return new GroupByNode([], null, true, allToken.Span);
        Consume(TokenType.Having);
        var having = new HavingNode(ComposeOperations());

        return new GroupByNode([], having, true, allToken.Span);
    }

    private SyntaxException GroupByAllWithExplicitFields(TextSpan span) => new(
        "GROUP BY ALL cannot be combined with explicit GROUP BY fields.",
        _lexer.AlreadyResolvedQueryPart, DiagnosticCode.MQ2001_UnexpectedToken, span);
}
