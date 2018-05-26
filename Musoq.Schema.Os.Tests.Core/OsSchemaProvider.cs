namespace Musoq.Schema.Os.Tests.Core
{
    internal class OsSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new OsSchema();
        }
    }
}