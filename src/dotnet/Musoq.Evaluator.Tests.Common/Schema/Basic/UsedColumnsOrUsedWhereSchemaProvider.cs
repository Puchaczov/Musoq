using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.Basic;

public class UsedColumnsOrUsedWhereSchemaProvider<T>(IDictionary<string, IEnumerable<T>> values) : ISchemaProvider
    where T : UsedColumnsOrUsedWhereEntity
{
    public ISchema GetSchema(string schema)
    {
        return new GenericSchema<UsedColumnsOrUsedWhereEntity, UsedColumnsOrUsedWhereTable>(values[schema],
            UsedColumnsOrUsedWhereEntity.TestNameToIndexMap, UsedColumnsOrUsedWhereEntity.TestIndexToObjectAccessMap);
    }
}
