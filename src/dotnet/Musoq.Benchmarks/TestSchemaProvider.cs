using Musoq.Schema;

namespace Musoq.Benchmarks;

public class TestSchemaProvider(List<TestEntity> entities) : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        return new TestSchema(entities);
    }
}
