using System;

namespace Musoq.Parser.Lexing
{
    public class UnknownTokenException : Exception
    {
        public UnknownTokenException(int position, char c, string s)
        {
        }
    }
}