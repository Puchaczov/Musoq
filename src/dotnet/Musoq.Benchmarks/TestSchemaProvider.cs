using Musoq.Schema;

namespace Musoq.Benchmarks;

public class TestSchemaProvider : ISchemaProvider
{
    private readonly List<TestEntity> _entities;

    public TestSchemaProvider(List<TestEntity> entities)
    {
        _entities = entities;
    }

    public ISchema GetSchema(string schema)
    {
        return new TestSchema(_entities);
    }
}
