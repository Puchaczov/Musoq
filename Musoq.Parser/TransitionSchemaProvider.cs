using System;
using System.Collections.Generic;
using System.Text;
using Musoq.Schema;

namespace Musoq.Parser
{
    public class TransitionSchemaProvider : ISchemaProvider
    {
        private readonly Dictionary<string, ISchema> _transientSchemas = new Dictionary<string, ISchema>();
        private readonly ISchemaProvider _schemaProvider;

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
