using Musoq.Schema.DataSources;

namespace Musoq.Benchmarks;

public class TableTestRowSource(List<TableTestEntity> entities) : RowSource<TableTestEntity>
{
    private readonly IReadOnlyList<IReadOnlyList<TableTestEntity>> _chunks = BenchmarkSourceChunks.Create(entities);

    public override IEnumerable<IReadOnlyList<TableTestEntity>> Chunks => _chunks;
}
