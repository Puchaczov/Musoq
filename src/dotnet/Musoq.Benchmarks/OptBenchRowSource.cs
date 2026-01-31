using Musoq.Schema.DataSources;

namespace Musoq.Benchmarks;

public class OptBenchRowSource : RowSource
{
    private readonly List<OptBenchEntity> _data;

    public OptBenchRowSource(List<OptBenchEntity> data)
    {
        _data = data;
    }

    public override IEnumerable<IObjectResolver> Rows
    {
        get
        {
            foreach (var entity in _data) yield return new OptBenchEntityResolver(entity);
        }
    }
}
