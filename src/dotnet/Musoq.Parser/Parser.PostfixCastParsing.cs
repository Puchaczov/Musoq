using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private Token ConsumeCastTypeName()
    {
        using var sourceRegion = _sourceCapture?.Begin(Syntax.SyntaxKind.Identifier, Current);
        if (Current.TokenType == TokenType.Identifier)
            return ConsumeAndGetToken(TokenType.Identifier);

        throw new SyntaxException(
            "Expected cast target type name after '::'.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2001_UnexpectedToken,
            Current.Span);
    }
}
