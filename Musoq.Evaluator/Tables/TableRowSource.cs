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

    public TableRowSource(Table rowSource, bool discardContext)
    {
        _table = rowSource;
        _discardedContext = discardContext;

        _columnToIndexMap = new Dictionary<string, int>();

        foreach (var column in _table.Columns)
            _columnToIndexMap.Add(column.ColumnName, column.ColumnIndex);
    }

    public override IEnumerable<IObjectResolver> Rows =>
        _discardedContext ? RowsWithDiscardedContexts : RowsWithContexts;

    private IEnumerable<IObjectResolver> RowsWithContexts =>
        _table.Select(row => new RowResolver(row, _columnToIndexMap));

    private IEnumerable<IObjectResolver> RowsWithDiscardedContexts =>
        _table.Select(row => new RowResolver(new ObjectsRow(row.Values, DiscardedContexts), _columnToIndexMap));

    private class DiscardedRowContext;
}