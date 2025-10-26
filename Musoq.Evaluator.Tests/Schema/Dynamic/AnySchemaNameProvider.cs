using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Tests.Schema.Dynamic;

public class AnySchemaNameProvider(
    IReadOnlyDictionary<string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)> schemas,
    Func<RuntimeContext, SchemaMethodInfo[]> getRawConstructors = null,
    Func<string, RuntimeContext, SchemaMethodInfo[]> getRawConstructorsByName = null
)
    : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        var schemaObj = schemas.Keys.First();
        return new DynamicSchema(
            schemas[schemaObj].Schema, 
            schemas[schemaObj].Values,
            getRawConstructors,
            getRawConstructorsByName);
    }
}
