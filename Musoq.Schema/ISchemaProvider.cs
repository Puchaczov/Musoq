namespace Musoq.Schema;

public interface ISchemaProvider
{
    ISchema GetSchema(string schema);
}