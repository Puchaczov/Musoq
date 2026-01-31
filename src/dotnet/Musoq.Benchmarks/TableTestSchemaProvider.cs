using Musoq.Schema;

namespace Musoq.Benchmarks;

public class TableTestSchemaProvider : ISchemaProvider
{
    private readonly List<TableTestEntity> _entities;

    public TableTestSchemaProvider(List<TableTestEntity> entities)
    {
        _entities = entities;
    }

    public ISchema GetSchema(string schema)
    {
        return new TableTestSchema(_entities);
    }
}
