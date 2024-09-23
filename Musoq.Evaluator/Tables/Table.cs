using Musoq.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Tables;

public class Table : IndexedList<Key, Row>, IEnumerable<Row>, IReadOnlyTable
{
    private readonly Dictionary<int, Column> _columnsByIndex;
    private readonly Dictionary<string, List<Column>> _columnsByName;

    public Table(string name, Column[] columns)
    {
        Name = name;

        _columnsByIndex = new Dictionary<int, Column>();
        _columnsByName = new Dictionary<string, List<Column>>();

        AddColumns(columns);
    }

    public string Name { get; }

    public IEnumerable<Column> Columns => _columnsByIndex.Values;

    public IEnumerator<Row> CurrentEnumerator { get; private set; }

    IReadOnlyList<IReadOnlyRow> IReadOnlyTable.Rows => Rows;

    public IEnumerator<Row> GetEnumerator()
    {
        CurrentEnumerator = Rows.GetEnumerator();
        return CurrentEnumerator;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void AddColumns(params Column[] columns)
    {
        foreach (var column in columns)
        {
            _columnsByIndex.Add(column.ColumnIndex, column);

            if (_columnsByName.TryGetValue(column.ColumnName, out var value))
            {
                var firstValue = value.First();

                if (firstValue.ColumnType != column.ColumnType)
                    throw new NotSupportedException(
                        $"({nameof(AddColumns)}) Mismatched types. {firstValue.ColumnType.Name} is not assignable from {column.ColumnType.Name}");
                    
                value.Add(column);
                continue;
            }
                
            _columnsByName.Add(column.ColumnName, [column]);
        }
    }

    public void Add(Row value)
    {
        if (value.Count != _columnsByIndex.Count)
            throw new NotSupportedException(
                $"({nameof(Add)}) Current row has {value.Count} values but {_columnsByIndex.Count} required.");

        for (var i = 0; i < value.Count; i++)
        {
            if (value[i] == null)
                continue;

            var t1 = value[i].GetType();
            var t2 = _columnsByIndex[i].ColumnType;
            if (!t2.IsAssignableFrom(t1))
                throw new NotSupportedException(
                    $"({nameof(Add)}) Mismatched types. {t2.Name} is not assignable from {t1.Name}");
        }

        Rows.Add(value);
    }
}