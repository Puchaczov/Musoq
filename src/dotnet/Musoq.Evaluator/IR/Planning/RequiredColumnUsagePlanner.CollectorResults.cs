using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class RequiredColumnUsagePlanner
{
    private sealed partial class RequiredColumnUsageCollector
    {
        private void AddColumn(
            string alias,
            string columnName,
            RequiredColumnUsageReason reason)
        {
            AddCteColumn(alias, columnName);

            var sourceReferences = _sources.Find(alias);
            if (sourceReferences.Length == 0)
            {
                AddRequiredColumn(alias, columnName);
                return;
            }

            var matchingSourceReferences = sourceReferences
                .Where(source => source.ContainsOutputColumn(columnName))
                .ToArray();

            if (matchingSourceReferences.Length == 0)
                return;

            AddRequiredColumn(alias, columnName);

            var confidence = matchingSourceReferences.Length == 1
                ? PlanningConfidence.High
                : PlanningConfidence.Medium;

            foreach (var source in matchingSourceReferences)
                AddSourceUsage(source, columnName, reason, confidence);
        }

        private void AddRequiredColumn(string alias, string columnName)
        {
            if (!_requiredColumnsByAlias.TryGetValue(alias, out var columns))
            {
                columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _requiredColumnsByAlias[alias] = columns;
            }

            columns.Add(columnName);
        }

        private void AddCteColumn(string alias, string columnName)
        {
            var references = _cteReferences.Find(alias);
            if (references.Length == 0)
                return;

            foreach (var reference in references)
            {
                if (!reference.ContainsOutputColumn(columnName))
                    continue;

                if (!_requiredColumnsByCteName.TryGetValue(reference.CteName, out var columns))
                {
                    columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    _requiredColumnsByCteName[reference.CteName] = columns;
                }

                columns.Add(columnName);
            }
        }

        private void AddSourceUsage(
            SourceReference source,
            string columnName,
            RequiredColumnUsageReason reason,
            PlanningConfidence confidence)
        {
            if (string.IsNullOrWhiteSpace(source.SourceContextId))
                return;

            if (!_usagesBySourceId.TryGetValue(source.SourceContextId, out var usages))
            {
                usages = new Dictionary<RequiredColumnUsageKey, RequiredColumnUsage>();
                _usagesBySourceId[source.SourceContextId] = usages;
            }

            var key = new RequiredColumnUsageKey(source.SourceContextId, source.Alias, columnName, reason);
            usages[key] = new RequiredColumnUsage(
                source.SourceContextId,
                source.Alias,
                columnName,
                reason,
                confidence);
        }

        private Dictionary<string, RequiredColumnUsage[]> CreateUsagesBySourceId()
        {
            var result = new Dictionary<string, RequiredColumnUsage[]>(StringComparer.Ordinal);

            foreach (var item in _usagesBySourceId.OrderBy(static item => item.Key, StringComparer.Ordinal))
            {
                result[item.Key] = item.Value.Values
                    .OrderBy(static usage => usage.ColumnName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static usage => usage.UsageReason.ToString(), StringComparer.Ordinal)
                    .ThenBy(static usage => usage.Alias, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            return result;
        }

        private Dictionary<string, IReadOnlySet<string>> CreateRequiredColumnsByAlias()
        {
            var result = new Dictionary<string, IReadOnlySet<string>>(_requiredColumnsByAlias.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var item in _requiredColumnsByAlias.OrderBy(static item => item.Key, StringComparer.OrdinalIgnoreCase))
            {
                result[item.Key] = new HashSet<string>(item.Value, StringComparer.OrdinalIgnoreCase);
            }

            return result;
        }

        private List<PlanningDecision> CreateDecisions(
            Dictionary<string, RequiredColumnUsage[]> usagesBySourceId)
        {
            var decisions = new List<PlanningDecision>();

            foreach (var source in _sources.All.OrderBy(static source => source.SourceContextId, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(source.SourceContextId))
                    continue;

                usagesBySourceId.TryGetValue(source.SourceContextId, out var usages);
                var usageCount = usages?.Length ?? 0;
                decisions.Add(new PlanningDecision(
                    PlanningDecisionCategory.RequiredColumns,
                    "RequiredColumnUsage",
                    source.SourceContextId,
                    usageCount == 0 ? "None" : "Derived",
                    usageCount == 0 ? PlanningConfidence.Low : PlanningConfidence.High,
                    CreateDecisionReason(source, usageCount)));
            }

            return decisions;
        }

        private static string CreateDecisionReason(SourceReference source, int usageCount)
        {
            return usageCount == 0
                ? $"No required column usage was proven for alias {source.Alias}."
                : $"Derived {usageCount} required column usage(s) for alias {source.Alias}.";
        }
    }
}
