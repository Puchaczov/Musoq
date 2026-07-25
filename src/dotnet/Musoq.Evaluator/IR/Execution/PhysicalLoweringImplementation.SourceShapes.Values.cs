using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static ValuesRowShape CreateValuesRowShape(PhysicalValuesScanNode values)
    {
        var usedFieldNames = new HashSet<string>(StringComparer.Ordinal);
        var fields = values.OutputSchema.Columns.Select(column => new FieldBinding(
            column.Name,
            $"{values.Alias}.{column.Name}",
            column.Index,
            column.Type,
            FieldNullability.Unknown,
            new GeneratedFieldAccess(CreateGeneratedFieldName(column.Name, column.Index, usedFieldNames)))).ToArray();

        return new ValuesRowShape(
            values.Alias,
            new GeneratedRowShape(CreateValuesRowTypeName(values), fields));
    }

    private static string CreateValuesRowTypeName(PhysicalValuesScanNode values)
    {
        return ExecutionSymbolicNamePolicy.CreateValuesRowTypeName(values.Alias, ComputeValuesShapeHash(values));
    }

    private static uint ComputeValuesShapeHash(PhysicalValuesScanNode values)
    {
        unchecked
        {
            var hash = 2166136261u;
            Add(values.Alias);
            foreach (var column in values.OutputSchema.Columns)
            {
                Add(column.Name);
                Add(column.Type.FullName ?? column.Type.Name);
            }

            return hash;

            void Add(string value)
            {
                foreach (var character in value)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }
            }
        }
    }
}
