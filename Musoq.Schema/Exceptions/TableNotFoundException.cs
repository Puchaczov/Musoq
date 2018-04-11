using System;

namespace Musoq.Schema.Exceptions
{
    public class TableNotFoundException : Exception
    {
        public TableNotFoundException(string table) 
            : base(table)
        { }
    }
}
