using Musoq.Schema.DataSources;

namespace Musoq.Benchmarks;

public class TableTestRowSource : RowSource
{
    private readonly List<TableTestEntity> _entities;

    public TableTestRowSource(List<TableTestEntity> entities)
    {
        _entities = entities;
    }

    public override IEnumerable<IObjectResolver> Rows
    {
        get
        {
            foreach (var entity in _entities) yield return new TableTestObjectResolver(entity);
        }
    }
}
