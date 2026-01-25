using Musoq.Schema;

namespace Musoq.Converter.Tests.Schema;

public class SystemSchemaProvider : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        return new SystemSchema();
    }
}
