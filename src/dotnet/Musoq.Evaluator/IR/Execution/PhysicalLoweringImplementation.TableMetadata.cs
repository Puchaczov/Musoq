using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static ExecutionCreateTable CreateTable(
        ExecutionVariable table,
        GeneratedRowShape rowShape,
        ExecutionCapacityHint? capacityHint = null)
    {
        return new ExecutionCreateTable(
            table,
            rowShape,
            capacityHint,
            CreateColumnMetadata(table.Name, rowShape.Fields, ExecutionColumnMetadataKind.TableColumns));
    }

    private static ExecutionColumnMetadata CreateColumnMetadata(
        string referenceName,
        IReadOnlyList<FieldBinding> fields,
        ExecutionColumnMetadataKind kind)
    {
        return new ExecutionColumnMetadata(
            referenceName,
            fields
                .Select(static field => ExecutionColumnMetadataFields.FromFieldBinding(field))
                .ToArray(),
            kind);
    }

    private static Dictionary<string, int> CreateCteIndexes(PhysicalCteNode cte)
    {
        var indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < cte.Definitions.Length; index++)
            indexes[cte.Definitions[index].Name] = index;

        return indexes;
    }

    private static Dictionary<string, int> CreateCteDefinitionSchemaFromIndexes(PhysicalCteNode cte)
    {
        var indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var schemaFromIndex = DefaultSchemaFromIndex;

        foreach (var definition in cte.Definitions)
        {
            indexes[definition.Name] = schemaFromIndex;
            schemaFromIndex += CountSchemaScans(definition.Plan);
        }

        return indexes;
    }
}
