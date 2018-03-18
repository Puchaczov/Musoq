using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musoq.Service.Exceptions
{
    public class SchemaNotFoundException : Exception
    {
        public SchemaNotFoundException(string message) 
            : base(message)
        { }
    }
}
