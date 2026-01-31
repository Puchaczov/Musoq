using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tables;

public class TableRowSource : RowSource
{
    private static readonly object[] DiscardedContexts = [new DiscardedRowContext()];

    private readonly IDictionary<string, int> _columnToIndexMap;
    private readonly bool _discardedContext;
    private readonly Table _table;
    private List<IObjectResolver>? _cachedResolvers;

    public TableRowSource(Table rowSource, bool discardContext)
    {
        _table = rowSource;
        _discardedContext = discardContext;

        _columnToIndexMap = new Dictionary<string, int>();

        foreach (var column in _table.Columns)
            _columnToIndexMap.Add(column.ColumnName, column.ColumnIndex);
    }

    public override IEnumerable<IObjectResolver> Rows
    {
        get
        {
            if (_cachedResolvers != null)
                return _cachedResolvers;

            _cachedResolvers = _discardedContext
                ? _table.Select(row =>
                        (IObjectResolver)new RowResolver(new ObjectsRow(row.Values, DiscardedContexts),
                            _columnToIndexMap))
                    .ToList()
                : _table.Select(row => (IObjectResolver)new RowResolver(row, _columnToIndexMap)).ToList();

            return _cachedResolvers;
        }
    }

    private class DiscardedRowContext;
}
