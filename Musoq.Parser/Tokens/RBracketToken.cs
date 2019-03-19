using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing
{
    public class RBracketToken : Token
    {
        public const string TokenText = "}";

        public RBracketToken(TextSpan textSpan)
            : base(TokenText, TokenType.RBracket, textSpan)
        {
        }
    }
}