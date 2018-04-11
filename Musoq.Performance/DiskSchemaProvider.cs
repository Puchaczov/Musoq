using Musoq.Schema;
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
