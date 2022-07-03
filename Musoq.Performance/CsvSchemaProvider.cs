using Musoq.Schema;
using Musoq.Schema.Csv;

namespace Musoq.Performance.Core
{
    internal class CsvSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new CsvSchema();
        }
    }
}