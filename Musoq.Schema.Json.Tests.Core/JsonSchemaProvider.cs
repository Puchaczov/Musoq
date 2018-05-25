namespace Musoq.Schema.Json.Tests.Core
{
    internal class JsonSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new JsonSchema();
        }
    }
}