using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical;
using System.Threading;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class RequiredColumnUsagePlanner
{
    public static RequiredColumnUsageResult Plan(LogicalNode logicalPlan)
    {
        return Plan(logicalPlan, CancellationToken.None);
    }

    public static RequiredColumnUsageResult Plan(
        LogicalNode logicalPlan,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(logicalPlan);
        cancellationToken.ThrowIfCancellationRequested();
        var sources = SourceReferenceIndex.Create(logicalPlan, cancellationToken);
        var cteReferences = CteReferenceIndex.Create(logicalPlan, cancellationToken);
        var collector = new RequiredColumnUsageCollector(sources, cteReferences, cancellationToken);
        collector.Collect(logicalPlan);
        cancellationToken.ThrowIfCancellationRequested();

        return collector.CreateResult();
    }

    private sealed partial class RequiredColumnUsageCollector(
        SourceReferenceIndex sources,
        CteReferenceIndex cteReferences,
        CancellationToken cancellationToken)
    {
        private readonly SourceReferenceIndex _sources = sources;
        private readonly CteReferenceIndex _cteReferences = cteReferences;
        private readonly CancellationToken _cancellationToken = cancellationToken;
        private readonly Dictionary<string, HashSet<string>> _requiredColumnsByAlias = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> _requiredColumnsByCteName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<RequiredColumnUsageKey, RequiredColumnUsage>> _usagesBySourceId = new(StringComparer.Ordinal);
    }
}
