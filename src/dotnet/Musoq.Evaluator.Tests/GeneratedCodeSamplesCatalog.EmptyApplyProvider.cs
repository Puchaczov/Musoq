using System;
using Musoq.Evaluator.Tests.Schema.Generated;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static ISchemaProvider CreateGeneratedEmptyChildApplySchemaProvider()
    {
        return new GeneratedEmptyChildApplySchemaProvider();
    }

    private sealed class GeneratedEmptyChildApplySchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!string.Equals(schema, "applyempty", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(schema, "#applyempty", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(schema);
            }

            return new GeneratedApplySampleSchema(
            [
                new GeneratedApplySampleEntity
                {
                    Name = "empty",
                    Numbers = []
                }
            ]);
        }
    }
}
