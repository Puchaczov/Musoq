using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Schema.Exceptions
{
    public class TableNotFoundException : Exception
    {
        public TableNotFoundException(string table) 
            : base(table)
        { }
    }
}
