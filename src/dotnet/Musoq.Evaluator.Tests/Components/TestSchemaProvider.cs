using System;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Simple test schema provider that returns a mock schema or throws if not found.
/// </summary>
public class TestSchemaProvider : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        throw new NotImplementedException($"Schema '{schema}' not found in test provider");
    }
}
