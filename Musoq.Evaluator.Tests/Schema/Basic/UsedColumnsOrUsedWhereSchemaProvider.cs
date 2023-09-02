using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.Basic;

public class UsedColumnsOrUsedWhereSchemaProvider<T> : ISchemaProvider
    where T : UsedColumnsOrUsedWhereEntity
{
    private readonly IDictionary<string, IEnumerable<T>> _values;

    public UsedColumnsOrUsedWhereSchemaProvider(IDictionary<string, IEnumerable<T>> values)
    {
        _values = values;
    }
        
    public ISchema GetSchema(string schema)
    {
        return new GenericSchema<UsedColumnsOrUsedWhereEntity, UsedColumnsOrUsedWhereTable>(_values[schema], UsedColumnsOrUsedWhereEntity.TestNameToIndexMap, UsedColumnsOrUsedWhereEntity.TestIndexToObjectAccessMap);
    }
}