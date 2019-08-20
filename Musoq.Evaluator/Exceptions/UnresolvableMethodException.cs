using System;
using System.Collections.Generic;
using System.Text;

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
