namespace Musoq.Parser.Tokens;

public class UnionAllToken(TextSpan span) : SetOperatorToken(UnionAllOperatorText, TokenType.UnionAll, span);
