using System;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.TemporarySchemas;

public class TableMetadataSource : RowSource
{
    private static readonly IReadOnlyDictionary<string, int> NameToIndexMap;
    private static readonly IReadOnlyDictionary<int, Func<object[], object>> IndexToObjectAccessMap;
    private readonly ISchemaColumn[] _columns;

    static TableMetadataSource()
    {
        NameToIndexMap = new Dictionary<string, int>
        {
            { nameof(ISchemaColumn.ColumnName), 0 },
            { nameof(ISchemaColumn.ColumnIndex), 1 },
            { nameof(ISchemaColumn.ColumnType), 2 }
        };

        IndexToObjectAccessMap = new Dictionary<int, Func<object[], object>>
        {
            { 0, items => items[0] },
            { 1, items => items[1] },
            { 2, items => items[2] }
        };
    }

    public TableMetadataSource(ISchemaColumn[] columns)
    {
        _columns = columns;
    }

    public override IEnumerable<IObjectResolver> Rows
    {
        get
        {
            foreach (var item in _columns)
            {
                var obj = new object[]
                {
                    item.ColumnName,
                    item.ColumnIndex,
                    item.ColumnType.Name
                };
                yield return new EntityResolver<object[]>(obj, NameToIndexMap, IndexToObjectAccessMap);
            }
        }
    }
}