using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.Exceptions;

namespace Musoq.Evaluator.Tests.Schema.Basic;

public class BasicSchemaProvider<T>(IDictionary<string, IEnumerable<T>> values) : ISchemaProvider
    where T : BasicEntity
{
    protected readonly IDictionary<string, IEnumerable<T>> Values = values;

    public virtual ISchema GetSchema(string schema)
    {
        if (!Values.TryGetValue(schema, out var value))
            throw new SourceNotFoundException($"Schema '{schema}' was not found.");

        return new GenericSchema<BasicEntity, BasicEntityTable>(value, BasicEntity.TestNameToIndexMap,
            BasicEntity.TestIndexToObjectAccessMap);
    }
}
