using Musoq.Benchmarks.Exceptions;
using Musoq.Schema;

namespace Musoq.Benchmarks.Schema;

public class GenericSchemaProvider<TEntity, TTable>(IDictionary<string, IEnumerable<TEntity>> values, IDictionary<string, int> nameToIndexMap, IDictionary<int, Func<TEntity, object>> indexToObjectAccessMap) : ISchemaProvider
{
    public virtual ISchema GetSchema(string schema)
    {
        if (values.TryGetValue(schema, out var value) == false)
            throw new SchemaNotFoundException ($"Schema {schema} not found.");

        return new GenericSchema<TEntity, TTable>(value, nameToIndexMap, indexToObjectAccessMap);
    }
}