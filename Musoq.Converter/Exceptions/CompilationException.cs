using System;

namespace Musoq.Converter.Exceptions
{
    public class CompilationException : Exception
    {
        public CompilationException(string message)
            : base(message)
        {
        }
    }
}