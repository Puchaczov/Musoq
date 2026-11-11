using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.IR.Planning.SourcePlanning;
using Musoq.Parser;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning;

internal static class SourceTransferPlanner
{
    public static SourceTransferPlanningResult Plan(
        PlanningContext context,
        SourcePlanningFacts sourcePlanning,
        IReadOnlyDictionary<string, SourceComputedTransferPlan>? computedTransfers = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(sourcePlanning);
        context.CancellationToken.ThrowIfCancellationRequested();

        var plans = new Dictionary<string, SourceTransferStrategyPlan>(StringComparer.Ordinal);
        var decisions = new List<PlanningDecision>();
        var usageResult = SourceTransferUsagePlanner.Plan(
            context.LogicalPlan,
            sourcePlanning,
            context.CancellationToken);
        context.CancellationToken.ThrowIfCancellationRequested();
        decisions.AddRange(usageResult.Decisions);

        foreach (var source in GetOrderedSources(sourcePlanning.SourcesById.Values, context.CancellationToken))
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var usage = usageResult.PlansBySourceId[source.SourceContextId];
            var computed = computedTransfers?.GetValueOrDefault(source.SourceContextId);
            var accepted = sourcePlanning.SourcePlanResultsBySourceId.GetValueOrDefault(source.SourceContextId)
                ?.AcceptedComputedProjections.Count ?? 0;
            if (accepted != (computed?.Bindings.Count ?? 0))
                throw new SourcePlanContractException("Accepted computed projections lost their planner bindings.", TextSpan.Empty);
            var plan = PlanSource(context, sourcePlanning, source, usage, computed);
            if (plan.Mode != SourceTransferMode.QueryScopedRows &&
                accepted > 0)
                throw new SourcePlanContractException("Accepted computed projections require query-scoped row transfer.", TextSpan.Empty);
            plans[source.SourceContextId] = plan;
            decisions.Add(CreateDecision(source, plan));
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        return new SourceTransferPlanningResult(plans, decisions);
    }

    private static SourceTransferStrategyPlan PlanSource(
        PlanningContext context,
        SourcePlanningFacts sourcePlanning,
        SourcePlanProperties source,
        SourceTransferUsagePlan usage,
        SourceComputedTransferPlan? computed)
    {
        if (!sourcePlanning.SourceDescriptorsBySourceId.TryGetValue(source.SourceContextId, out var descriptor))
            return SourceTransferStrategyPlan.DeclaredRows(source.SourceContextId, "source descriptor was unavailable");

        context.CancellationToken.ThrowIfCancellationRequested();
        var logicalEnumColumn = FindLogicalScalarEnumColumn(
            sourcePlanning,
            source,
            descriptor,
            context.CancellationToken);
        if (logicalEnumColumn != null)
        {
            var required = SourceTransferCapabilities.QueryScopedRows |
                           SourceTransferCapabilities.LogicalScalarReads;
            if ((descriptor.TransferCapabilities & required) != required ||
                (context.TargetSourceTransferCapabilities & required) != required)
            {
                throw new EnumSourceCapabilityException(
                    $"{descriptor.Identity.SchemaName}.{descriptor.Identity.MethodName}",
                    logicalEnumColumn.ColumnName,
                    ResolveColumnSpan(sourcePlanning, source.SourceContextId, logicalEnumColumn.ColumnName));
            }
        }

        if (!descriptor.TransferCapabilities.HasFlag(SourceTransferCapabilities.QueryScopedRows))
        {
            return SourceTransferStrategyPlan.DeclaredRows(
                source.SourceContextId,
                "source did not advertise query-scoped rows");
        }

        if (!context.TargetSourceTransferCapabilities.HasFlag(SourceTransferCapabilities.QueryScopedRows))
        {
            return SourceTransferStrategyPlan.DeclaredRows(
                source.SourceContextId,
                "selected execution target does not support query-scoped rows");
        }

        if (usage.RowRequirement == SourceRowRequirement.DeclaredEntity)
            return SourceTransferStrategyPlan.DeclaredRows(source.SourceContextId, usage.RowRequirementReason);

        context.CancellationToken.ThrowIfCancellationRequested();
        if (!TryCreateShape(
                sourcePlanning,
                source,
                descriptor,
                computed,
                context.CancellationToken,
                out var shape,
                out var shapeReason))
        {
            if (descriptor.RowType == typeof(QueryScopedRowContract))
                throw new SourcePlanContractException(
                    $"Native source '{source.SourceContextId}' cannot bind its concrete row shape: {shapeReason}.", TextSpan.Empty);
            return SourceTransferStrategyPlan.DeclaredRows(source.SourceContextId, shapeReason);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        var estimatedPayload = SourceRowPayload.Estimate(shape, context.CancellationToken);
        var carrier = estimatedPayload <= SourceRowPayload.StructCarrierLimit && usage.Lifetime == SourceRowLifetime.ScanLocal
            ? SourceQueryRowCarrier.ReadonlyStruct
            : SourceQueryRowCarrier.SealedClass;
        var carrierName = carrier == SourceQueryRowCarrier.ReadonlyStruct ? "readonly struct" : "sealed class";
        var reason = string.Create(
            CultureInfo.InvariantCulture,
            $"query-scoped rows selected with {carrierName} carrier; lifetime={usage.Lifetime}; shape={shape.Fingerprint}; estimated payload={estimatedPayload} bytes; {usage.LifetimeReason}");
        if (computed is { UnusedSourceColumns.Count: > 0 })
            reason += $"; dropped provider-only inputs: {string.Join(", ", computed.UnusedSourceColumns.Order(StringComparer.Ordinal))}";

        return new SourceTransferStrategyPlan(
            source.SourceContextId,
            SourceTransferMode.QueryScopedRows,
            carrier,
            shape,
            reason)
        {
            ComputedProjections = computed?.Bindings ?? [],
            Lifetime = usage.Lifetime == SourceRowLifetime.ScanLocal
                ? SourceQueryRowLifetime.ScanLocal
                : SourceQueryRowLifetime.EscapesScan
        };
    }

    private static bool TryCreateShape(
        SourcePlanningFacts sourcePlanning,
        SourcePlanProperties source,
        SourceDescriptor descriptor,
        SourceComputedTransferPlan? computed,
        CancellationToken cancellationToken,
        out QueryRowShape shape,
        out string reason)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryResolveColumns(
                sourcePlanning,
                source,
                descriptor,
                cancellationToken,
                out var columns,
                out reason))
        {
            shape = null!;
            return false;
        }

        ISchemaColumn? volatileColumn = null;
        foreach (var column in columns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (column.Stability == ColumnStability.Volatile)
            {
                volatileColumn = column;
                break;
            }
        }
        if (volatileColumn != null)
        {
            shape = null!;
            reason = $"query-row transfer would freeze volatile column '{volatileColumn.ColumnName}'; retained declared-row fallback";
            return false;
        }

        var fields = new List<QueryRowField>(columns.Length);
        var names = new HashSet<string>(StringComparer.Ordinal);
        var sourceIndexes = new HashSet<int>();

        foreach (var column in GetOrderedColumns(columns, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (column.ColumnIndex < 0)
            {
                shape = null!;
                reason = $"column '{column.ColumnName}' has an invalid ordinal {column.ColumnIndex}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(column.ColumnName))
            {
                shape = null!;
                reason = "source columns contain an empty name";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(column.IntendedTypeName))
            {
                shape = null!;
                reason = $"column '{column.ColumnName}' has an unresolved intended type name";
                return false;
            }

            if (column.ColumnType == typeof(object))
            {
                shape = null!;
                reason = $"column '{column.ColumnName}' has only the object runtime type";
                return false;
            }

            if (!QueryRowField.IsSupportedFieldType(column.ColumnType))
            {
                shape = null!;
                reason = $"column '{column.ColumnName}' has an unusable CLR type '{column.ColumnType}'";
                return false;
            }

            if (!names.Add(column.ColumnName))
            {
                shape = null!;
                reason = $"source columns contain duplicate name '{column.ColumnName}'";
                return false;
            }

            if (!sourceIndexes.Add(column.ColumnIndex))
            {
                shape = null!;
                reason = $"source columns contain duplicate ordinal {column.ColumnIndex}";
                return false;
            }

            if (computed?.UnusedSourceColumns.Contains(column.ColumnName) == true)
                continue;
            fields.Add(new QueryRowField(
                fields.Count,
                column.ColumnIndex,
                column.ColumnName,
                column.ColumnType,
                column.SourceReadType,
                column.EnumType,
                IsNullable(column.ColumnType),
                column.ReadModifiers,
                column.Stability));
        }

        if (computed is not null)
            foreach (var binding in computed.Bindings)
                fields.Add(QueryRowField.ForComputedProjection(fields.Count, binding.Projection));
        shape = new QueryRowShape(fields);
        reason = string.Empty;
        cancellationToken.ThrowIfCancellationRequested();
        return true;
    }

    private static bool TryResolveColumns(
        SourcePlanningFacts sourcePlanning,
        SourcePlanProperties source,
        SourceDescriptor descriptor,
        CancellationToken cancellationToken,
        out ISchemaColumn[] columns,
        out string reason)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (source.QueryRowProjection.State == SourceProjectionState.Exact)
        {
            columns = source.QueryRowProjection.Columns.ToArray();
            reason = string.Empty;
            cancellationToken.ThrowIfCancellationRequested();
            return true;
        }

        if (sourcePlanning.ProjectedSchemaColumnsBySourceId.TryGetValue(source.SourceContextId, out var projected) &&
            projected.Length > 0)
        {
            columns = projected;
            reason = string.Empty;
            cancellationToken.ThrowIfCancellationRequested();
            return true;
        }

        if (source.ProjectedSchemaColumns.Length > 0)
        {
            columns = source.ProjectedSchemaColumns;
            reason = string.Empty;
            cancellationToken.ThrowIfCancellationRequested();
            return true;
        }

        if (sourcePlanning.SourceInteractionPlansBySourceId.TryGetValue(source.SourceContextId, out var interaction) &&
            interaction.QuerySourceColumns.Length > 0)
        {
            columns = interaction.QuerySourceColumns;
            reason = string.Empty;
            cancellationToken.ThrowIfCancellationRequested();
            return true;
        }

        var describedColumns = descriptor.Columns.ToArray();
        cancellationToken.ThrowIfCancellationRequested();
        if (HasAmbiguousMetadata(describedColumns, cancellationToken, out reason))
        {
            columns = [];
            return false;
        }

        if (source.RequiredColumns.Length == 0)
        {
            columns = describedColumns;
            reason = string.Empty;
            cancellationToken.ThrowIfCancellationRequested();
            return true;
        }

        var describedByName = describedColumns
            .Select(static column => column.ColumnName)
            .ToHashSet(
            StringComparer.Ordinal);
        cancellationToken.ThrowIfCancellationRequested();
        var missing = new List<string>();
        foreach (var required in source.RequiredColumns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!describedByName.Contains(required))
                missing.Add(required);
        }

        missing.Sort((left, right) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return StringComparer.Ordinal.Compare(left, right);
        });
        if (missing.Count > 0)
        {
            columns = [];
            reason = $"required source columns were unresolved: {string.Join(", ", missing)}";
            return false;
        }

        var requiredNames = new HashSet<string>(source.RequiredColumns, StringComparer.Ordinal);
        var selectedColumns = new List<ISchemaColumn>();
        foreach (var column in describedColumns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (requiredNames.Contains(column.ColumnName))
                selectedColumns.Add(column);
        }

        columns = selectedColumns.ToArray();
        reason = string.Empty;
        cancellationToken.ThrowIfCancellationRequested();
        return true;
    }

    private static bool HasAmbiguousMetadata(
        ISchemaColumn[] columns,
        CancellationToken cancellationToken,
        out string reason)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        var ordinals = new HashSet<int>();
        foreach (var column in columns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!names.Add(column.ColumnName))
            {
                reason = $"source columns contain duplicate name '{column.ColumnName}'";
                return true;
            }

            if (!ordinals.Add(column.ColumnIndex))
            {
                reason = $"source columns contain duplicate ordinal {column.ColumnIndex}";
                return true;
            }
        }

        reason = string.Empty;
        cancellationToken.ThrowIfCancellationRequested();
        return false;
    }

    private static bool IsNullable(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }

    private static bool RequiresLogicalScalarRead(ISchemaColumn column)
    {
        if (column.EnumType == null)
            return false;

        var sourceType = Nullable.GetUnderlyingType(column.SourceReadType) ?? column.SourceReadType;
        return !sourceType.IsEnum;
    }

    private static ISchemaColumn? FindLogicalScalarEnumColumn(
        SourcePlanningFacts sourcePlanning,
        SourcePlanProperties source,
        SourceDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (TryResolveColumns(
                sourcePlanning,
                source,
                descriptor,
                cancellationToken,
                out var resolvedColumns,
                out _))
        {
            foreach (var column in resolvedColumns)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (RequiresLogicalScalarRead(column))
                    return column;
            }
        }

        foreach (var column in source.QueryRowProjection.Columns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (RequiresLogicalScalarRead(column))
                return column;
        }

        foreach (var column in source.ProjectedSchemaColumns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (RequiresLogicalScalarRead(column))
                return column;
        }

        foreach (var column in descriptor.Columns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (RequiresLogicalScalarRead(column))
                return column;
        }

        if (sourcePlanning.ProjectedSchemaColumnsBySourceId.TryGetValue(source.SourceContextId, out var projected))
        {
            foreach (var column in projected)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (RequiresLogicalScalarRead(column))
                    return column;
            }
        }

        if (sourcePlanning.SourceInteractionPlansBySourceId.TryGetValue(source.SourceContextId, out var interaction))
        {
            foreach (var column in interaction.QuerySourceColumns)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (RequiresLogicalScalarRead(column))
                    return column;
            }
        }

        return null;
    }

    private static TextSpan ResolveColumnSpan(
        SourcePlanningFacts sourcePlanning,
        string sourceContextId,
        string columnName)
    {
        return sourcePlanning.SourceContractDiagnosticLocationsBySourceId.TryGetValue(sourceContextId, out var locations) &&
               locations.TryGetColumnSpan(columnName, out var span)
            ? span
            : TextSpan.Empty;
    }

    private static IReadOnlyList<SourcePlanProperties> GetOrderedSources(
        IEnumerable<SourcePlanProperties> sources,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ordered = sources.ToList();
        cancellationToken.ThrowIfCancellationRequested();
        ordered.Sort((left, right) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return StringComparer.Ordinal.Compare(left.SourceContextId, right.SourceContextId);
        });
        cancellationToken.ThrowIfCancellationRequested();
        return ordered;
    }

    private static IReadOnlyList<ISchemaColumn> GetOrderedColumns(
        IEnumerable<ISchemaColumn> columns,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ordered = columns.ToList();
        cancellationToken.ThrowIfCancellationRequested();
        ordered.Sort((left, right) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return left.ColumnIndex.CompareTo(right.ColumnIndex);
        });
        cancellationToken.ThrowIfCancellationRequested();
        return ordered;
    }

    private static PlanningDecision CreateDecision(
        SourcePlanProperties source,
        SourceTransferStrategyPlan plan)
    {
        var outcome = plan.Mode == SourceTransferMode.QueryScopedRows ? "Selected" : "Fallback";
        var confidence = plan.Mode == SourceTransferMode.QueryScopedRows
            ? PlanningConfidence.High
            : PlanningConfidence.Medium;
        return new PlanningDecision(
            PlanningDecisionCategory.SourcePlanning,
            "SourceTransfer",
            source.SourceContextId,
            outcome,
            confidence,
            plan.Reason);
    }
}
