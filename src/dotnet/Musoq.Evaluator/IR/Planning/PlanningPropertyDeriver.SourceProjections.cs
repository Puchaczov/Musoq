using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PlanningPropertyDeriver
{
    private static Dictionary<string, ISchemaColumn[]> CreateProjectedSchemaColumns(
        IReadOnlyDictionary<string, SourcePlanProperties> sources,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = new Dictionary<string, ISchemaColumn[]>(StringComparer.Ordinal);

        foreach (var source in sources.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (source.QueryRowProjection.State == SourceProjectionState.Unavailable &&
                source.ProjectedColumns.Length == 0)
                continue;

            result[source.SourceContextId] = source.QueryRowProjection.State == SourceProjectionState.Exact
                ? source.QueryRowProjection.Columns.ToArray()
                : source.ProjectedSchemaColumns;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    private static Dictionary<string, SourcePlanProperties> CreateSourceProperties(
        PlanningContext context,
        IReadOnlyList<SchemaScanNode> scans,
        IReadOnlyDictionary<string, IReadOnlySet<string>> requiredColumnsByAlias,
        IReadOnlyDictionary<string, IrExpression[]> pushedPredicates,
        List<PlanningDecision> decisions)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        var sources = new Dictionary<string, SourcePlanProperties>(StringComparer.Ordinal);

        foreach (var scan in scans)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(scan.SourceContextId))
                continue;

            var requiredColumns = requiredColumnsByAlias.TryGetValue(scan.Alias, out var required)
                ? required.OrderBy(static name => name, StringComparer.OrdinalIgnoreCase).ToArray()
                : [];
            context.CancellationToken.ThrowIfCancellationRequested();
            var predicates = pushedPredicates.TryGetValue(scan.SourceContextId, out var pushed)
                ? pushed
                : [];
            var projection = TryCreateProjectedColumns(
                context,
                scan,
                requiredColumns,
                decisions,
                out var projectedSchemaColumns,
                out var queryRowProjection,
                out var confidence,
                out var reason);
            context.CancellationToken.ThrowIfCancellationRequested();

            sources[scan.SourceContextId] = new SourcePlanProperties(
                scan.SourceContextId,
                scan.Alias,
                scan.SchemaName,
                scan.MethodName,
                requiredColumns,
                predicates,
                projection,
                projectedSchemaColumns,
                queryRowProjection,
                confidence,
                reason);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        return sources;
    }

    private static string[] TryCreateProjectedColumns(
        PlanningContext context,
        SchemaScanNode scan,
        string[] requiredColumns,
        List<PlanningDecision> decisions,
        out ISchemaColumn[] projectedSchemaColumns,
        out SourceQueryRowProjection queryRowProjection,
        out PlanningConfidence confidence,
        out string reason)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        projectedSchemaColumns = [];
        confidence = PlanningConfidence.Low;
        queryRowProjection = SourceQueryRowProjection.Unavailable(
            "Exact query-row projection metadata was unavailable.");

        if (requiredColumns.Length == 0)
        {
            reason = "No source columns are required; the query-row projection is exactly empty.";
            queryRowProjection = SourceQueryRowProjection.Exact([], reason);
            AddProjectionDecision(decisions, scan, "Skipped", confidence, reason);
            return [];
        }

        var entityType = SourceEntityMetadataResolver.ResolveSourceEntityType(context.Scope, scan.Alias);
        context.CancellationToken.ThrowIfCancellationRequested();
        if (entityType == null)
        {
            reason = "Source entity type could not be resolved from metadata.";
            AddProjectionDecision(decisions, scan, "Skipped", confidence, reason);
            return [];
        }

        if (SourceEntityMetadataResolver.IsDynamicEntity(entityType))
        {
            reason = $"Source entity type {entityType.Name} is dynamic.";
            AddProjectionDecision(decisions, scan, "Skipped", confidence, reason);
            return [];
        }

        var availableColumns = ResolveAvailableColumns(context, scan);
        context.CancellationToken.ThrowIfCancellationRequested();
        if (availableColumns.Length == 0)
        {
            reason = "Source columns could not be resolved.";
            AddProjectionDecision(decisions, scan, "Skipped", confidence, reason);
            return [];
        }

        var availableByName = new Dictionary<string, ISchemaColumn>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var column in availableColumns)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            availableByName.Add(column.ColumnName, column);
        }
        context.CancellationToken.ThrowIfCancellationRequested();

        foreach (var requiredColumn in requiredColumns)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (!availableByName.ContainsKey(requiredColumn))
            {
                reason = "At least one required column was not present in source metadata.";
                AddProjectionDecision(decisions, scan, "Skipped", confidence, reason);
                return [];
            }
        }

        var requiredSet = new HashSet<string>(requiredColumns, StringComparer.OrdinalIgnoreCase);
        var projectedList = new List<ISchemaColumn>();
        foreach (var column in availableColumns)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (requiredSet.Contains(column.ColumnName))
                projectedList.Add(column);
        }

        var projected = projectedList.ToArray();
        var projectedColumnNames = new string[projected.Length];
        for (var index = 0; index < projected.Length; index++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            projectedColumnNames[index] = projected[index].ColumnName;
        }
        queryRowProjection = SourceQueryRowProjection.Exact(
            projected,
            $"Resolved an exact {projected.Length}-column query-row projection.");

        confidence = PlanningConfidence.High;
        if (projected.Length >= availableColumns.Length)
        {
            reason = "All known source columns are required.";
            AddProjectionDecision(decisions, scan, "Skipped", confidence, reason);
            return [];
        }

        projectedSchemaColumns = projected;
        reason = $"Retained {projected.Length} of {availableColumns.Length} known source column(s).";
        AddProjectionDecision(decisions, scan, "Applied", confidence, reason);
        context.CancellationToken.ThrowIfCancellationRequested();
        return projectedColumnNames;
    }

    private static void AddProjectionDecision(
        List<PlanningDecision> decisions,
        SchemaScanNode scan,
        string outcome,
        PlanningConfidence confidence,
        string reason)
    {
        decisions.Add(new PlanningDecision(
            PlanningDecisionCategory.ProjectionPruning,
            "SourceProjection",
            FormatSource(scan),
            outcome,
            confidence,
            reason));
    }

    private static ISchemaColumn[] ResolveAvailableColumns(PlanningContext context, SchemaScanNode scan)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        if (context.InferredColumns.TryGetValue(scan.Alias, out var inferredColumns) && inferredColumns.Length > 0)
            return inferredColumns;

        var columns = new ISchemaColumn[scan.OutputSchema.Columns.Length];
        for (var index = 0; index < scan.OutputSchema.Columns.Length; index++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            columns[index] = scan.OutputSchema.Columns[index].ToSchemaColumn();
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        return columns;
    }
}
