namespace FQL.Parser.Tokens
{
    public class WordToken : Token
    {
        public const string TokenText = "word";

        public WordToken(string value, TextSpan span)
            : base(value, TokenType.Word, span)
        {
        }
    }
}