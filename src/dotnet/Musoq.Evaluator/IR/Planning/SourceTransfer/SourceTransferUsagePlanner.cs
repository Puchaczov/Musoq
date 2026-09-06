using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using System.Threading;

namespace Musoq.Evaluator.IR.Planning;

internal static class SourceTransferUsagePlanner
{
    public static SourceTransferUsagePlanningResult Plan(
        LogicalNode logicalPlan,
        SourcePlanningFacts sourcePlanning)
    {
        return Plan(logicalPlan, sourcePlanning, CancellationToken.None);
    }

    public static SourceTransferUsagePlanningResult Plan(
        LogicalNode logicalPlan,
        SourcePlanningFacts sourcePlanning,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(logicalPlan);
        ArgumentNullException.ThrowIfNull(sourcePlanning);
        cancellationToken.ThrowIfCancellationRequested();

        var reasonsBySourceId = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var sourceContextId in sourcePlanning.SourcesById.Keys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            reasonsBySourceId[sourceContextId] = new HashSet<string>(StringComparer.Ordinal);
        }

        var sourceReferences = SourceReferenceIndex.Create(logicalPlan, cancellationToken);
        var lifetimePlans = SourceTransferLifetimePlanner.Plan(logicalPlan, cancellationToken);

        foreach (var expression in LogicalExpressionTraversal.SelfAndDescendantExpressions(
                     logicalPlan,
                     cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var methodCall in IrExpressionTraversal.SelfAndDescendants(expression, cancellationToken)
                         .OfType<MethodCall>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                RecordDeclaredEntityUsage(
                    methodCall,
                    sourcePlanning,
                    sourceReferences,
                    reasonsBySourceId,
                    cancellationToken);
            }
        }

        var plans = new Dictionary<string, SourceTransferUsagePlan>(StringComparer.Ordinal);
        var decisions = new List<PlanningDecision>();
        foreach (var source in GetOrderedSources(sourcePlanning.SourcesById.Values, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var reasons = reasonsBySourceId[source.SourceContextId];
            var requiresDeclaredEntity = reasons.Count > 0;
            var reason = requiresDeclaredEntity
                ? string.Join("; ", reasons.OrderBy(static item => item, StringComparer.Ordinal))
                : $"Alias '{source.Alias}' is used through column values only.";
            var lifetime = lifetimePlans.TryGetValue(source.SourceContextId, out var lifetimePlan)
                ? lifetimePlan
                : SourceTransferLifetimePlan.Escapes(
                    source.SourceContextId,
                    "No logical lifetime path was available for the source.");
            var plan = new SourceTransferUsagePlan(
                source.SourceContextId,
                requiresDeclaredEntity ? SourceRowRequirement.DeclaredEntity : SourceRowRequirement.ColumnValuesOnly,
                lifetime.Lifetime,
                reason,
                lifetime.Reason);
            plans[source.SourceContextId] = plan;
            decisions.Add(CreateDecision(plan));
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new SourceTransferUsagePlanningResult(plans, decisions);
    }

    private static void RecordDeclaredEntityUsage(
        MethodCall methodCall,
        SourcePlanningFacts sourcePlanning,
        SourceReferenceIndex sourceReferences,
        IReadOnlyDictionary<string, HashSet<string>> reasonsBySourceId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parameter = SourceInjectionMethodFacts.FindInjectedSourceParameter(methodCall.Method);
        if (parameter == null)
            return;

        var reason = $"Method '{methodCall.Method.Name}' requires the declared source entity.";
        if (!string.IsNullOrWhiteSpace(methodCall.Alias))
        {
            var references = sourceReferences.Find(methodCall.Alias);
            if (references.Length > 0)
            {
                foreach (var reference in references)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    AddReason(reference.SourceContextId, reason, reasonsBySourceId);
                }
                return;
            }

            AddReasonToAllSources(
                $"Method '{methodCall.Method.Name}' targets unresolved alias '{methodCall.Alias}', so declared entities are retained conservatively.",
                reasonsBySourceId,
                cancellationToken);
            return;
        }

        var candidates = new List<string>();
        foreach (var source in sourcePlanning.SourcesById.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (CanSupplyInjectedSource(source, parameter.ParameterType, sourcePlanning))
                candidates.Add(source.SourceContextId);
        }

        if (candidates.Count > 0)
        {
            foreach (var sourceContextId in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddReason(sourceContextId, reason, reasonsBySourceId);
            }
            return;
        }

        var hasIncompleteRowMetadata = false;
        foreach (var descriptor in sourcePlanning.SourceDescriptorsBySourceId.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (descriptor.RowType == null)
            {
                hasIncompleteRowMetadata = true;
                break;
            }
        }

        if (hasIncompleteRowMetadata)
        {
            AddReasonToAllSources(
                $"Method '{methodCall.Method.Name}' has an unaliased source injection with incomplete row metadata.",
                reasonsBySourceId,
                cancellationToken);
        }
    }

    private static bool CanSupplyInjectedSource(
        SourcePlanProperties source,
        Type parameterType,
        SourcePlanningFacts sourcePlanning)
    {
        if (!sourcePlanning.SourceDescriptorsBySourceId.TryGetValue(source.SourceContextId, out var descriptor) ||
            descriptor.RowType == null)
        {
            return false;
        }

        return parameterType == typeof(object) || parameterType.IsAssignableFrom(descriptor.RowType);
    }

    private static void AddReason(
        string? sourceContextId,
        string reason,
        IReadOnlyDictionary<string, HashSet<string>> reasonsBySourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceContextId) ||
            !reasonsBySourceId.TryGetValue(sourceContextId, out var reasons))
        {
            return;
        }

        reasons.Add(reason);
    }

    private static void AddReasonToAllSources(
        string reason,
        IReadOnlyDictionary<string, HashSet<string>> reasonsBySourceId,
        CancellationToken cancellationToken)
    {
        foreach (var reasons in reasonsBySourceId.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            reasons.Add(reason);
        }
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

    private static PlanningDecision CreateDecision(SourceTransferUsagePlan plan)
    {
        var declaredEntity = plan.RowRequirement == SourceRowRequirement.DeclaredEntity;
        return new PlanningDecision(
            PlanningDecisionCategory.SourcePlanning,
            "SourceTransferUsage",
            plan.SourceContextId,
            declaredEntity ? "DeclaredEntity" : "ColumnValuesOnly",
            PlanningConfidence.High,
            $"{plan.RowRequirementReason} {plan.LifetimeReason}");
    }
}
