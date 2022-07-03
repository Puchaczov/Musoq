namespace Musoq.Schema.Time
{
    public class TimeSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new TimeSchema();
        }
    }
}