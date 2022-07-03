namespace Musoq.Schema.SeparatedValues.Tests.Core
{
    internal class CsvSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new SeparatedValuesSchema();
        }
    }
}