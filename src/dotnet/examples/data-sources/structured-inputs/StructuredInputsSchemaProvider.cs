using Musoq.Schema;
using Musoq.Schema.Exceptions;

namespace Musoq.Examples.DataSources.StructuredInputs;

public sealed class StructuredInputsSchemaProvider : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (string.Equals(schema, StructuredInputsSchema.SchemaName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(schema, $"#{StructuredInputsSchema.SchemaName}", StringComparison.OrdinalIgnoreCase))
            return new StructuredInputsSchema();

        throw new SourceNotFoundException($"Structured input example schema does not expose '{schema}'.");
    }
}