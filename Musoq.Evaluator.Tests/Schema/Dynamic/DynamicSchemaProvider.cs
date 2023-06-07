
using System;
using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.Dynamic;

public class DynamicSchemaProvider : ISchemaProvider
{
    private readonly
        IReadOnlyDictionary<string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)> _schemas;
    
    public DynamicSchemaProvider(IReadOnlyDictionary<string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)> schemas)
    {
        _schemas = schemas;
    }

    public ISchema GetSchema(string schema)
    {
        return new DynamicSchema(_schemas[schema].Schema, _schemas[schema].Values);
    }
}