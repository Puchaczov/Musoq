using System;
using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.Dynamic;

public class DynamicSchemaProvider(
    IReadOnlyDictionary<string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)> schemas)
    : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        return new DynamicSchema(schemas[schema].Schema, schemas[schema].Values);
    }
}
