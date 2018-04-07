namespace Musoq.Schema.Json.Tests
{
    internal class JsonSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new JsonSchema();
        }
    }
}