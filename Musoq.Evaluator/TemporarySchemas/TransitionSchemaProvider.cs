using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.TemporarySchemas;

public class TransitionSchemaProvider : ISchemaProvider
{
    private readonly ISchemaProvider _schemaProvider;
    private readonly Dictionary<string, ISchema> _transientSchemas = new();

    public TransitionSchemaProvider(ISchemaProvider schema)
    {
        _schemaProvider = schema;
    }

    public ISchema GetSchema(string schema)
    {
        return _transientSchemas.TryGetValue(schema, out var foundSchema) ? foundSchema : _schemaProvider.GetSchema(schema);
    }

    public void AddTransitionSchema(ISchema schema)
    {
        _transientSchemas.Add(schema.Name, schema);
    }
}