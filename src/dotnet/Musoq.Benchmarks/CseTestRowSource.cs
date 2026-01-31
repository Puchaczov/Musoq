using Musoq.Schema.DataSources;

namespace Musoq.Benchmarks;

public class CseTestRowSource : RowSource
{
    private readonly IReadOnlyCollection<CseTestEntity> _data;

    public CseTestRowSource(IReadOnlyCollection<CseTestEntity> data)
    {
        _data = data;
    }

    public override IEnumerable<IObjectResolver> Rows
    {
        get
        {
            foreach (var entity in _data) yield return new CseTestEntityResolver(entity);
        }
    }
}
