using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Schema.Exceptions
{
    public class SourceNotFoundException : Exception
    {
        public SourceNotFoundException(string table)
            : base(table)
        { }
    }
}
