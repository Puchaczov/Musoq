using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.DataSourceProgress;

public class ReportingEntitySource<T> : RowSource<T> where T : BasicEntity
{
    private readonly string _dataSourceName;
    private readonly IEnumerable<T> _entities;
    private readonly SourceExecutionContext _runtimeContext;

    public ReportingEntitySource(
        IEnumerable<T> entities,
        IDictionary<string, int> nameToIndexMap,
        IDictionary<int, Func<T, object?>> indexToObjectAccessMap,
        SourceExecutionContext runtimeContext,
        string dataSourceName)
    {
        _ = nameToIndexMap;
        _ = indexToObjectAccessMap;

        _entities = entities;
        _runtimeContext = runtimeContext;
        _dataSourceName = dataSourceName;
    }

    public override IEnumerable<IReadOnlyList<T>> Chunks
    {
        get
        {
            _runtimeContext.ReportDataSourceBegin(_dataSourceName);

            var entityList = _entities.ToList();
            var totalRows = entityList.Count;

            _runtimeContext.ReportDataSourceRowsKnown(_dataSourceName, totalRows);

            yield return entityList;

            long rowsProcessed = 0;
            for (var index = 0; index < entityList.Count; index++)
            {
                rowsProcessed++;
                _runtimeContext.ReportDataSourceRowsRead(_dataSourceName, rowsProcessed, totalRows);
            }

            _runtimeContext.ReportDataSourceEnd(_dataSourceName, totalRows);
        }
    }
}
