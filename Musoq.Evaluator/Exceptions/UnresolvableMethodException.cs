using System;

namespace Musoq.Evaluator.Exceptions
{
    public class UnresolvableMethodException : Exception
    {
        public UnresolvableMethodException(string message)
            : base(message)
        {

        }
    }
}
