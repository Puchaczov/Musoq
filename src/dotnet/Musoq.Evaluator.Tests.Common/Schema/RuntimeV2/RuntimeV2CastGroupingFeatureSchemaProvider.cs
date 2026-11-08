using System;
using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.RuntimeV2;

public sealed class RuntimeV2CastGroupingFeatureSchemaProvider(IReadOnlyList<RuntimeV2CastGroupingFeatureEntity> rows)
    : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (!string.Equals(schema, "features", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(schema, "#features", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(schema);

        return new RuntimeV2CastGroupingFeatureSchema(rows);
    }
}
