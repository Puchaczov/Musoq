namespace Musoq.Parser.Tokens;

public sealed class StringConcatenationToken(TextSpan span) : Token("||", TokenType.StringConcatenation, span);
