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
            .GroupBy(static column => column.ColumnName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.OrdinalIgnoreCase);

        return usages
            .Select(static usage => usage.ColumnName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static columnName => columnName, StringComparer.OrdinalIgnoreCase)
            .Select(columnName => columnsByName.TryGetValue(columnName, out var column)
                ? new SourceColumnRef(column.ColumnName, column.ReadModifiers)
                : new SourceColumnRef(columnName))
            .ToArray();
    }
}
