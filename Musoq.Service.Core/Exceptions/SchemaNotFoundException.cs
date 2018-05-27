using System;

namespace Musoq.Service.Core.Exceptions
{
    public class SchemaNotFoundException : Exception
    {
        public SchemaNotFoundException(string message)
            : base(message)
        {
        }
    }
}