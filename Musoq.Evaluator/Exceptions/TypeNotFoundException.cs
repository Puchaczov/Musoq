using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Evaluator.Exceptions
{
    public class TypeNotFoundException : Exception
    {
        public TypeNotFoundException(string message)
            : base(message)
        {

        }
    }
}
