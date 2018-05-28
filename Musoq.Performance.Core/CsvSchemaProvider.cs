using Musoq.Schema;
using Musoq.Schema.Csv;

namespace Musoq.Performance
{
    internal class CsvSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new CsvSchema();
        }
    }
}