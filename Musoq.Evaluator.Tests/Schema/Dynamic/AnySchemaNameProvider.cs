using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.Dynamic;

public class AnySchemaNameProvider : ISchemaProvider
{
    private readonly
        IReadOnlyDictionary<string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)> _schemas;
    
    public AnySchemaNameProvider(IReadOnlyDictionary<string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)> schemas)
    {
        _schemas = schemas;
    }

    public ISchema GetSchema(string schema)
    {
        var schemaObj = _schemas.Keys.First();
        return new DynamicSchema(_schemas[schemaObj].Schema, _schemas[schemaObj].Values);
    }
}
