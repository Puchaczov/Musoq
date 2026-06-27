namespace Musoq.Parser.Tokens;

public class CommentToken(string value, TextSpan span) : Token(value, TokenType.Comment, span);
