namespace Musoq.Schema.Csv.Tests
{
    internal class CsvSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new CsvSchema();
        }
    }
}