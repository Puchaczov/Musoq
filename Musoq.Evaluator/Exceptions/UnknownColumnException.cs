using System;

namespace Musoq.Evaluator.Exceptions
{
    public class UnknownColumnException : Exception
    {
        public UnknownColumnException(string message)
            : base(message)
        {
        }
    }
}