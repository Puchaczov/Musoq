using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.EnvironmentVariable;

public class EnvironmentVariablesSchemaProvider : ISchemaProvider
{
    private readonly IDictionary<string, IEnumerable<EnvironmentVariableEntity>> _values;

    public EnvironmentVariablesSchemaProvider(IDictionary<string, IEnumerable<EnvironmentVariableEntity>> values)
    {
        _values = values;
    }

    public ISchema GetSchema(string schema)
    {
        return new EnvironmentVariablesSchema(_values[schema]);
    }
}