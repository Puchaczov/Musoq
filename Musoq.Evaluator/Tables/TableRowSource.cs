using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tables;

public class TableRowSource : RowSource
{
    private readonly IDictionary<string, int> _columnToIndexMap;
    private readonly Table _table;
    private readonly bool _skipContext;

    public TableRowSource(Table rowSource, bool skipContext)
    {
        _table = rowSource;
        _skipContext = skipContext;
        
        _columnToIndexMap = new Dictionary<string, int>();

        foreach (var column in _table.Columns)
            _columnToIndexMap.Add(column.ColumnName, column.ColumnIndex);
    }

    public override IEnumerable<IObjectResolver> Rows => _skipContext ? RowsWithSkippedContexts : RowsWithContexts;
    
    private IEnumerable<IObjectResolver> RowsWithContexts =>
        _table.Select(row => new RowResolver((ObjectsRow)row, _columnToIndexMap));
    
    private IEnumerable<IObjectResolver> RowsWithSkippedContexts =>
        _table.Select(row => new RowResolver(new ObjectsRow(row.Values, row.Values), _columnToIndexMap));
}