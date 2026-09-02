using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.IR.Planning;

internal static class SourceTransferPlanner
{
    private const int StructCarrierPayloadLimit = 64;

    public static SourceTransferPlanningResult Plan(
        PlanningContext context,
        SourcePlanningFacts sourcePlanning)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(sourcePlanning);

        var plans = new Dictionary<string, SourceTransferStrategyPlan>(StringComparer.Ordinal);
        var decisions = new List<PlanningDecision>();
        var usageResult = SourceTransferUsagePlanner.Plan(context.LogicalPlan, sourcePlanning);
        decisions.AddRange(usageResult.Decisions);

        foreach (var source in sourcePlanning.SourcesById.Values.OrderBy(static source => source.SourceContextId, StringComparer.Ordinal))
        {
            var usage = usageResult.PlansBySourceId[source.SourceContextId];
            var plan = PlanSource(context, sourcePlanning, source, usage);
            plans[source.SourceContextId] = plan;
            decisions.Add(CreateDecision(source, plan));
        }

        return new SourceTransferPlanningResult(plans, decisions);
    }

    private static SourceTransferStrategyPlan PlanSource(
        PlanningContext context,
        SourcePlanningFacts sourcePlanning,
        SourcePlanProperties source,
        SourceTransferUsagePlan usage)
    {
        if (!sourcePlanning.SourceDescriptorsBySourceId.TryGetValue(source.SourceContextId, out var descriptor))
            return SourceTransferStrategyPlan.Legacy(source.SourceContextId, "source descriptor was unavailable");

        var logicalEnumColumn = FindLogicalScalarEnumColumn(sourcePlanning, source, descriptor);
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
            return SourceTransferStrategyPlan.Legacy(
                source.SourceContextId,
                "source did not advertise query-scoped rows");
        }

        if (!context.TargetSourceTransferCapabilities.HasFlag(SourceTransferCapabilities.QueryScopedRows))
        {
            return SourceTransferStrategyPlan.Legacy(
                source.SourceContextId,
                "selected execution target does not support query-scoped rows");
        }

        if (usage.RowRequirement == SourceRowRequirement.DeclaredEntity)
            return SourceTransferStrategyPlan.Legacy(source.SourceContextId, usage.RowRequirementReason);

        if (!TryCreateShape(sourcePlanning, source, descriptor, out var shape, out var shapeReason))
            return SourceTransferStrategyPlan.Legacy(source.SourceContextId, shapeReason);

        var estimatedPayload = EstimatePayload(shape);
        var carrier = estimatedPayload is <= StructCarrierPayloadLimit && usage.Lifetime == SourceRowLifetime.ScanLocal
            ? SourceQueryRowCarrier.ReadonlyStruct
            : SourceQueryRowCarrier.SealedClass;
        var carrierName = carrier == SourceQueryRowCarrier.ReadonlyStruct ? "readonly struct" : "sealed class";
        var reason = string.Create(
            CultureInfo.InvariantCulture,
            $"query-scoped rows selected with {carrierName} carrier; lifetime={usage.Lifetime}; shape={shape.Fingerprint}; estimated payload={estimatedPayload} bytes; {usage.LifetimeReason}");

        return new SourceTransferStrategyPlan(
            source.SourceContextId,
            SourceTransferMode.QueryScopedRows,
            carrier,
            shape,
            reason)
        {
            Lifetime = usage.Lifetime == SourceRowLifetime.ScanLocal
                ? SourceQueryRowLifetime.ScanLocal
                : SourceQueryRowLifetime.EscapesScan
        };
    }

    private static bool TryCreateShape(
        SourcePlanningFacts sourcePlanning,
        SourcePlanProperties source,
        SourceDescriptor descriptor,
        out QueryRowShape shape,
        out string reason)
    {
        if (!TryResolveColumns(sourcePlanning, source, descriptor, out var columns, out reason))
        {
            shape = null!;
            return false;
        }

        var volatileColumn = columns.FirstOrDefault(static column => column.Stability == ColumnStability.Volatile);
        if (volatileColumn != null)
        {
            shape = null!;
            reason = $"query-row transfer would freeze volatile column '{volatileColumn.ColumnName}'; retained declared-row fallback";
            return false;
        }

        var fields = new List<QueryRowField>(columns.Length);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sourceIndexes = new HashSet<int>();

        foreach (var column in columns.OrderBy(static column => column.ColumnIndex))
        {
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

        shape = new QueryRowShape(fields);
        reason = string.Empty;
        return true;
    }

    private static bool TryResolveColumns(
        SourcePlanningFacts sourcePlanning,
        SourcePlanProperties source,
        SourceDescriptor descriptor,
        out ISchemaColumn[] columns,
        out string reason)
    {
        if (source.QueryRowProjection.State == SourceProjectionState.Exact)
        {
            columns = source.QueryRowProjection.Columns.ToArray();
            reason = string.Empty;
            return true;
        }

        if (sourcePlanning.ProjectedSchemaColumnsBySourceId.TryGetValue(source.SourceContextId, out var projected) &&
            projected.Length > 0)
        {
            columns = projected;
            reason = string.Empty;
            return true;
        }

        if (source.ProjectedSchemaColumns.Length > 0)
        {
            columns = source.ProjectedSchemaColumns;
            reason = string.Empty;
            return true;
        }

        if (sourcePlanning.SourceInteractionPlansBySourceId.TryGetValue(source.SourceContextId, out var interaction) &&
            interaction.QuerySourceColumns.Length > 0)
        {
            columns = interaction.QuerySourceColumns;
            reason = string.Empty;
            return true;
        }

        var describedColumns = descriptor.Columns.ToArray();
        if (HasAmbiguousMetadata(describedColumns, out reason))
        {
            columns = [];
            return false;
        }

        if (source.RequiredColumns.Length == 0)
        {
            columns = describedColumns;
            reason = string.Empty;
            return true;
        }

        var describedByName = describedColumns.ToDictionary(
            static column => column.ColumnName,
            StringComparer.OrdinalIgnoreCase);
        var missing = source.RequiredColumns
            .Where(required => !describedByName.ContainsKey(required))
            .OrderBy(static required => required, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (missing.Length > 0)
        {
            columns = [];
            reason = $"required source columns were unresolved: {string.Join(", ", missing)}";
            return false;
        }

        var requiredNames = new HashSet<string>(source.RequiredColumns, StringComparer.OrdinalIgnoreCase);
        columns = describedColumns
            .Where(column => requiredNames.Contains(column.ColumnName))
            .ToArray();
        reason = string.Empty;
        return true;
    }

    private static bool HasAmbiguousMetadata(ISchemaColumn[] columns, out string reason)
    {
        var duplicateName = columns
            .GroupBy(static column => column.ColumnName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicateName != null)
        {
            reason = $"source columns contain duplicate name '{duplicateName.Key}'";
            return true;
        }

        var duplicateOrdinal = columns
            .GroupBy(static column => column.ColumnIndex)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicateOrdinal != null)
        {
            reason = $"source columns contain duplicate ordinal {duplicateOrdinal.Key}";
            return true;
        }

        reason = string.Empty;
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
        SourceDescriptor descriptor)
    {
        if (TryResolveColumns(sourcePlanning, source, descriptor, out var resolvedColumns, out _))
        {
            var resolved = resolvedColumns.FirstOrDefault(RequiresLogicalScalarRead);
            if (resolved != null)
                return resolved;
        }

        IEnumerable<ISchemaColumn> candidates = source.QueryRowProjection.Columns
            .Concat(source.ProjectedSchemaColumns)
            .Concat(descriptor.Columns);
        if (sourcePlanning.ProjectedSchemaColumnsBySourceId.TryGetValue(source.SourceContextId, out var projected))
            candidates = candidates.Concat(projected);
        if (sourcePlanning.SourceInteractionPlansBySourceId.TryGetValue(source.SourceContextId, out var interaction))
            candidates = candidates.Concat(interaction.QuerySourceColumns);

        return candidates.FirstOrDefault(RequiresLogicalScalarRead);
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

    private static int EstimatePayload(QueryRowShape shape)
    {
        var size = 0;
        foreach (var field in shape.Fields)
            size += EstimateFieldSize(field.FieldType);

        return size;
    }

    private static int EstimateFieldSize(Type type)
    {
        if (!type.IsValueType || Nullable.GetUnderlyingType(type) != null)
            return IntPtr.Size;

        if (type.IsEnum)
            type = Enum.GetUnderlyingType(type);

        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean or TypeCode.Byte or TypeCode.SByte => 1,
            TypeCode.Char or TypeCode.Int16 or TypeCode.UInt16 => 2,
            TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Single => 4,
            TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Double or TypeCode.DateTime => 8,
            _ when type == typeof(decimal) || type == typeof(Guid) => 16,
            _ => StructCarrierPayloadLimit + 1
        };
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
