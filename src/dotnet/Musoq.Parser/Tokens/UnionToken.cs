namespace Musoq.Parser.Tokens;

public class UnionToken(TextSpan span) : SetOperatorToken(UnionOperatorText, TokenType.Union, span);
