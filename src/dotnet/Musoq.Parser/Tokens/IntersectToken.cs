namespace Musoq.Parser.Tokens;

public class IntersectToken(TextSpan span) : SetOperatorToken(IntersectOperatorText, TokenType.Intersect, span);
