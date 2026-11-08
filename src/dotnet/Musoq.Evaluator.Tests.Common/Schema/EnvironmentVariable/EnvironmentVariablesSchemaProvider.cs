using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.EnvironmentVariable;

public class EnvironmentVariablesSchemaProvider : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        return new EnvironmentVariablesSchema();
    }
}
