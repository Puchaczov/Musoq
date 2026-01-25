using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.DataSourceProgress;

public class ReportingEntitySource<T> : RowSource where T : BasicEntity
{
    private readonly string _dataSourceName;
    private readonly IEnumerable<T> _entities;
    private readonly IDictionary<int, Func<T, object>> _indexToObjectAccessMap;
    private readonly IDictionary<string, int> _nameToIndexMap;
    private readonly RuntimeContext _runtimeContext;

    public ReportingEntitySource(
        IEnumerable<T> entities,
        IDictionary<string, int> nameToIndexMap,
        IDictionary<int, Func<T, object>> indexToObjectAccessMap,
        RuntimeContext runtimeContext,
        string dataSourceName)
    {
        _entities = entities;
        _nameToIndexMap = nameToIndexMap;
        _indexToObjectAccessMap = indexToObjectAccessMap;
        _runtimeContext = runtimeContext;
        _dataSourceName = dataSourceName;
    }

    public override IEnumerable<IObjectResolver> Rows
    {
        get
        {
            _runtimeContext.ReportDataSourceBegin(_dataSourceName);

            var entityList = _entities.ToList();
            var totalRows = entityList.Count;

            _runtimeContext.ReportDataSourceRowsKnown(_dataSourceName, totalRows);

            long rowsProcessed = 0;
            foreach (var entity in entityList)
            {
                yield return new EntityResolver<T>(
                    entity,
                    _nameToIndexMap as IReadOnlyDictionary<string, int>,
                    _indexToObjectAccessMap as IReadOnlyDictionary<int, Func<T, object>>);

                rowsProcessed++;
                _runtimeContext.ReportDataSourceRowsRead(_dataSourceName, rowsProcessed, totalRows);
            }

            _runtimeContext.ReportDataSourceEnd(_dataSourceName, totalRows);
        }
    }
}
