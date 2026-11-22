using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning;

internal static class SourceScanMetadata
{
    internal static Type? ResolveEntityType(PlanningContext context, SchemaScanNode scan) =>
        scan.BoundTable is { } table
            ? table.EntityType
            : SourceEntityMetadataResolver.ResolveSourceEntityType(context.Scope, scan.Alias);

    internal static ISchemaColumn[] ResolveColumns(PlanningContext context, SchemaScanNode scan)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        if (scan.BoundTable is { } table)
            return table.Columns.ToArray();
        if (context.InferredColumns.TryGetValue(scan.Alias, out var inferredColumns) && inferredColumns.Length > 0)
            return inferredColumns;

        var columns = new ISchemaColumn[scan.OutputSchema.Columns.Length];
        for (var index = 0; index < columns.Length; index++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            columns[index] = scan.OutputSchema.Columns[index].ToSchemaColumn();
        }

        return columns;
    }
}
