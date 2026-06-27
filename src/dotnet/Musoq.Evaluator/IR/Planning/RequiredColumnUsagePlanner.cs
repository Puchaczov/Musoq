using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class RequiredColumnUsagePlanner
{
    public static RequiredColumnUsageResult Plan(LogicalNode logicalPlan)
    {
        ArgumentNullException.ThrowIfNull(logicalPlan);
        var sources = SourceReferenceIndex.Create(logicalPlan);
        var cteReferences = CteReferenceIndex.Create(logicalPlan);
        var collector = new RequiredColumnUsageCollector(sources, cteReferences);
        collector.Collect(logicalPlan);

        return collector.CreateResult();
    }

    private sealed partial class RequiredColumnUsageCollector(
        SourceReferenceIndex sources,
        CteReferenceIndex cteReferences)
    {
        private readonly SourceReferenceIndex _sources = sources;
        private readonly CteReferenceIndex _cteReferences = cteReferences;
        private readonly Dictionary<string, HashSet<string>> _requiredColumnsByAlias = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> _requiredColumnsByCteName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<RequiredColumnUsageKey, RequiredColumnUsage>> _usagesBySourceId = new(StringComparer.Ordinal);
    }
}
