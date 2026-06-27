using Musoq.Schema.DataSources;

namespace Musoq.Benchmarks;

public class TestRowSource(List<TestEntity> entities) : RowSource<TestEntity>
{
    private readonly IReadOnlyList<IReadOnlyList<TestEntity>> _chunks = BenchmarkSourceChunks.Create(entities);

    public override IEnumerable<IReadOnlyList<TestEntity>> Chunks => _chunks;
}
