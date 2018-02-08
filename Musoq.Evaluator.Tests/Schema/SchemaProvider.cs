using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema
{
    public class SchemaProvider<T> : ISchemaProvider
        where T : BasicEntity
    {
        private readonly IDictionary<string, IEnumerable<T>> _values;

        public SchemaProvider(IDictionary<string, IEnumerable<T>> values)
        {
            _values = values;
        }

        public ISchema GetSchema(string schema)
        {
            return new TestSchema<T>(_values[schema]);
        }
    }
}