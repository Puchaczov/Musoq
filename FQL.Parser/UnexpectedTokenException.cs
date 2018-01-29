using System;
using FQL.Parser.Tokens;

namespace FQL.Parser
{
    internal class UnexpectedTokenException<T> : Exception
    {
        public UnexpectedTokenException(int position, Token current)
        {
            throw new NotImplementedException();
        }
    }
}