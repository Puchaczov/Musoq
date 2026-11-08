using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.RuntimeV2;

public sealed class RuntimeV2RegressionSchemaProvider(IReadOnlyList<RuntimeV2RegressionEntity> rows)
    : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        return new RuntimeV2RegressionSchema(rows);
    }
}
