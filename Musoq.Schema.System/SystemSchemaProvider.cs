namespace Musoq.Schema.System
{
    public class SystemSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new SystemSchema();
        }
    }
}