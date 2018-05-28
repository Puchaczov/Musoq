using Musoq.Schema;
using Musoq.Schema.Os;

namespace Musoq.Performance
{
    internal class DiskSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new OsSchema();
        }
    }
}