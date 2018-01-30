using System;
using Musoq.Parser.Tokens;

namespace Musoq.Parser
{
    internal class UnexpectedTokenException<T> : Exception
    {
        public UnexpectedTokenException(int position, Token current)
        {
            throw new NotImplementedException();
        }
    }
}