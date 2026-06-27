using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.TemporarySchemas;

public class TransitionSchemaProvider(ISchemaProvider schemaProvider) : ISchemaProvider
{
    private readonly Dictionary<string, ISchema> _transientSchemas = new();

    public ISchema GetSchema(string schema)
    {
        return _transientSchemas.TryGetValue(schema, out var foundSchema)
            ? foundSchema
            : schemaProvider.GetSchema(schema);
    }

    public void AddTransitionSchema(ISchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        _transientSchemas.Add(schema.Name, schema);
    }
}
