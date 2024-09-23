using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.Basic;

public class BasicSchemaProvider<T> : ISchemaProvider
    where T : BasicEntity
{
    private readonly IDictionary<string, IEnumerable<T>> _values;

    public BasicSchemaProvider(IDictionary<string, IEnumerable<T>> values)
    {
        _values = values;
    }

    public ISchema GetSchema(string schema)
    {
        return new GenericSchema<BasicEntity, BasicEntityTable>(_values[schema], BasicEntity.TestNameToIndexMap, BasicEntity.TestIndexToObjectAccessMap);
    }
}