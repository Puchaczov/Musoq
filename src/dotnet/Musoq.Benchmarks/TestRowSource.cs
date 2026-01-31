using Musoq.Schema.DataSources;

namespace Musoq.Benchmarks;

public class TestRowSource : RowSource
{
    private readonly List<TestEntity> _entities;

    public TestRowSource(List<TestEntity> entities)
    {
        _entities = entities;
    }

    public override IEnumerable<IObjectResolver> Rows
    {
        get
        {
            foreach (var entity in _entities) yield return new TestObjectResolver(entity);
        }
    }
}
