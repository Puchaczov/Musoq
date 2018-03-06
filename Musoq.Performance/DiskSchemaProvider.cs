using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musoq.Schema;
using Musoq.Schema.Csv;
using Musoq.Schema.Disk;

namespace Musoq.Performance
{
    internal class DiskSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new DiskSchema();
        }
    }
}
