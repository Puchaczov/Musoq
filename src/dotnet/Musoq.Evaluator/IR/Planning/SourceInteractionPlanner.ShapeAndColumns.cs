using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Schema;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class SourceInteractionPlanner
{
    private static SourceInteractionShape ResolveShape(
        PlanningContext context,
        SchemaFromNode? sourceNode,
        SchemaScanNode scan)
    {
        if (sourceNode is Musoq.Evaluator.Parser.SchemaFromNode { HasExternallyProvidedTypes: true })
        {
            return new SourceInteractionShape(
                SourceShapeKind.ExternallyProvidedType,
                PlanningConfidence.High,
                "Source types were provided externally.");
        }

        var entityType = SourceEntityMetadataResolver.ResolveSourceEntityType(context.Scope, scan.Alias);
        if (entityType != null)
        {
            if (SourceEntityMetadataResolver.IsDynamicEntity(entityType))
            {
                return new SourceInteractionShape(
                    SourceShapeKind.Dynamic,
                    PlanningConfidence.Low,
                    $"Source entity type {entityType.Name} is dynamic.");
            }

            return new SourceInteractionShape(
                SourceShapeKind.KnownClr,
                PlanningConfidence.High,
                $"Source entity type {entityType.Name} was resolved from metadata.");
        }

        if (context.InferredColumns.TryGetValue(scan.Alias, out var inferredColumns) && inferredColumns.Length > 0)
        {
            return new SourceInteractionShape(
                SourceShapeKind.InferredMetadata,
                PlanningConfidence.Medium,
                $"Source shape was inferred from {inferredColumns.Length} column(s).");
        }

        return new SourceInteractionShape(
            SourceShapeKind.Unknown,
            PlanningConfidence.Low,
            "Source entity type and inferred metadata were unavailable.");
    }

    private static SourceInteractionColumns ResolveColumnContract(SourcePlanProperties source, ISchemaColumn[] usedColumns)
    {
        if (source.QueryRowProjection.State == SourceProjectionState.Exact)
        {
            var exactColumns = source.QueryRowProjection.Columns.ToArray();
            return new SourceInteractionColumns(
                SourceColumnContract.ProjectedColumns,
                exactColumns,
                PlanningConfidence.High,
                $"Query-row projection is exact with {exactColumns.Length} planned column(s).");
        }

        if (source.ProjectedSchemaColumns.Length > 0)
        {
            return new SourceInteractionColumns(
                SourceColumnContract.ProjectedColumns,
                source.ProjectedSchemaColumns,
                PlanningConfidence.High,
                $"Projection uses {source.ProjectedSchemaColumns.Length} planned column(s).");
        }

        if (source.ProjectedColumns.Length > 0)
        {
            var projectedNames = new HashSet<string>(source.ProjectedColumns, StringComparer.OrdinalIgnoreCase);
            var projectedColumns = usedColumns
                .Where(column => projectedNames.Contains(column.ColumnName))
                .ToArray();

            if (projectedColumns.Length == source.ProjectedColumns.Length)
            {
                return new SourceInteractionColumns(
                    SourceColumnContract.ProjectedColumns,
                    projectedColumns,
                    PlanningConfidence.Medium,
                    $"Projection resolved {projectedColumns.Length} planned column(s) from runtime metadata.");
            }
        }

        if (usedColumns.Length > 0)
        {
            return new SourceInteractionColumns(
                SourceColumnContract.FullColumns,
                usedColumns,
                PlanningConfidence.Medium,
                $"Runtime source info keeps {usedColumns.Length} used column(s).");
        }

        return new SourceInteractionColumns(
            SourceColumnContract.UnavailableColumns,
            [],
            PlanningConfidence.Low,
            "No source columns were available for runtime source info.");
    }
}
