namespace Musoq.Parser.Tokens;

public class CommentToken : Token
{
    public CommentToken(string value, TextSpan span)
        : base(value, TokenType.Comment, span)
    {
    }
}
