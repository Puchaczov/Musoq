using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.TemporarySchemas
{
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
            if (_transientSchemas.ContainsKey(schema))
                return _transientSchemas[schema];

            return _schemaProvider.GetSchema(schema);
        }

        public void AddTransitionSchema(ISchema schema)
        {
            _transientSchemas.Add(schema.Name, schema);
        }
    }
}