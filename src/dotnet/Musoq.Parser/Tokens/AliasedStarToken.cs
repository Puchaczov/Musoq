namespace Musoq.Parser.Tokens;

public class AliasedStarToken(string value, TextSpan span) : Token(value, TokenType.AliasedStar, span);
