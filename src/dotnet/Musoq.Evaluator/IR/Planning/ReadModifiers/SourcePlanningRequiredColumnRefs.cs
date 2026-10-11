using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning.SourcePlanning;

internal static partial class SourcePlanningPlanner
{
    private static SourceColumnRef[] ResolveRequiredColumns(
        PlanningContext context,
        SchemaScanNode scan,
        IReadOnlyDictionary<string, RequiredColumnUsage[]> requiredColumnUsagesBySourceId)
    {
        if (string.IsNullOrWhiteSpace(scan.SourceContextId) ||
            !requiredColumnUsagesBySourceId.TryGetValue(scan.SourceContextId, out var usages) ||
            usages.Length == 0)
        {
            return [];
        }

        var columnsByName = ResolveColumns(context, scan)
            .GroupBy(static column => column.ColumnName, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);

        return usages
            .Select(static usage => usage.ColumnName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static columnName => columnName, StringComparer.Ordinal)
            .Select(columnName => columnsByName.TryGetValue(columnName, out var column)
                ? new SourceColumnRef(column.ColumnName, column.ReadModifiers) { ValueTypeName = column.ColumnType.AssemblyQualifiedName }
                : new SourceColumnRef(columnName))
            .ToArray();
    }
}
