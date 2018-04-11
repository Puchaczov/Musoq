using System;

namespace Musoq.Service.Exceptions
{
    public class SchemaNotFoundException : Exception
    {
        public SchemaNotFoundException(string message) 
            : base(message)
        { }
    }
}
