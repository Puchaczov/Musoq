using System;

namespace Musoq.Evaluator.Exceptions
{
    public class RefreshMethodNotFoundException : Exception
    {
        public RefreshMethodNotFoundException(string message)
            : base(message)
        { }
    }
}
