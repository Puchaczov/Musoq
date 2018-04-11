using System;

namespace Musoq.Schema.Exceptions
{
    public class SourceNotFoundException : Exception
    {
        public SourceNotFoundException(string table)
            : base(table)
        { }
    }
}
