using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private WhereNode? ComposeWhere(bool withoutWhereToken)
    {
        using var sourceRegion = _sourceCapture?.Begin(Syntax.SyntaxKind.WhereClause, Current);
        if (Current.TokenType == TokenType.Where)
        {
            Consume(TokenType.Where);
            if (Current.TokenType == TokenType.EndOfFile)
                throw new SyntaxException(
                    "The WHERE clause ends unexpectedly at end of input.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2017_UnexpectedEndOfFile,
                    new TextSpan(Current.Span.Start, 0));
            return new WhereNode(ComposeOperations());
        }

        if (withoutWhereToken)
            return new WhereNode(ComposeOperations());

        return null;
    }
}
