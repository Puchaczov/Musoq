using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Parser.Tokens
{
    public class WithToken : Token
    {
        public const string TokenText = "with";

        public WithToken(TextSpan span) 
            : base(TokenText, TokenType.With, span)
        {
        }
    }
}
