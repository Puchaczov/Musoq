namespace Musoq.Parser.Tokens;

public class ExceptToken(TextSpan span) : SetOperatorToken(ExceptOperatorText, TokenType.Except, span);
