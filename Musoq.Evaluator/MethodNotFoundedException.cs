using System;
using System.Runtime.Serialization;

namespace Musoq.Evaluator
{
    [Serializable]
    internal class MethodNotFoundedException : Exception
    {
        public MethodNotFoundedException()
        {
        }

        public MethodNotFoundedException(string message) : base(message)
        {
        }

        public MethodNotFoundedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MethodNotFoundedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}