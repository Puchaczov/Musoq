using System.Collections.Generic;
using Musoq.Evaluator.Tests.Exceptions;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.Basic;

public class BasicSchemaProvider<T>(IDictionary<string, IEnumerable<T>> values) : ISchemaProvider
    where T : BasicEntity
{
    protected readonly IDictionary<string, IEnumerable<T>> Values = values;

    public virtual ISchema GetSchema(string schema)
    {
        if (!Values.TryGetValue(schema, out var value))
            throw new SchemaNotFoundException();

        return new GenericSchema<BasicEntity, BasicEntityTable>(value, BasicEntity.TestNameToIndexMap,
            BasicEntity.TestIndexToObjectAccessMap);
    }
}