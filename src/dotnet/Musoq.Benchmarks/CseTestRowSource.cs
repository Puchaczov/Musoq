using Musoq.Schema.DataSources;

namespace Musoq.Benchmarks;

public class CseTestRowSource(IReadOnlyList<CseTestEntity> data) : RowSource<CseTestEntity>
{
    private readonly IReadOnlyList<IReadOnlyList<CseTestEntity>> _chunks = BenchmarkSourceChunks.Create(data);

    public override IEnumerable<IReadOnlyList<CseTestEntity>> Chunks => _chunks;
}
