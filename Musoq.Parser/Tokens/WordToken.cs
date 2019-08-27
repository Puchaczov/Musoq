namespace Musoq.Parser.Tokens
{
    public class WordToken : Token
    {
        public const string TokenText = "word";

        public const string EmptyTokenText = "''";

        public WordToken(string value, TextSpan span)
            : base(value, TokenType.Word, span)
        {
        }
    }
}