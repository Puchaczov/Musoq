namespace FQL.Schema
{
    public interface ISchemaProvider
    {
        ISchema GetSchema(string schema);
    }
}