using System;
using Musoq.Parser.Tokens;

namespace Musoq.Parser
{
    internal class UnexpectedTokenException<T> : Exception
    {
        public UnexpectedTokenException(int position, Token current)
        : base($"Token {current.TokenType} at position {position} is unexpected.")
        { }
    }
}