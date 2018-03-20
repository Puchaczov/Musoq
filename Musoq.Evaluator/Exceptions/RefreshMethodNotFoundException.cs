using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Evaluator.Exceptions
{
    public class RefreshMethodNotFoundException : Exception
    {
        public RefreshMethodNotFoundException(string message)
            : base(message)
        { }
    }
}
