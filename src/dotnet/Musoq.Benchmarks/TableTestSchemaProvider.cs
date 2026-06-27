using Musoq.Schema;

namespace Musoq.Benchmarks;

public class TableTestSchemaProvider(List<TableTestEntity> entities) : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        return new TableTestSchema(entities);
    }
}
