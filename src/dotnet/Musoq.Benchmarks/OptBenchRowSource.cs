using Musoq.Schema.DataSources;

namespace Musoq.Benchmarks;

public class OptBenchRowSource(List<OptBenchEntity> data) : RowSource<OptBenchEntity>
{
    private readonly IReadOnlyList<IReadOnlyList<OptBenchEntity>> _chunks = BenchmarkSourceChunks.Create(data);

    public override IEnumerable<IReadOnlyList<OptBenchEntity>> Chunks => _chunks;
}
