using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;

namespace Musoq.Evaluator.IR.Planning;

internal static class SourceTransferUsagePlanner
{
    public static SourceTransferUsagePlanningResult Plan(
        LogicalNode logicalPlan,
        SourcePlanningFacts sourcePlanning)
    {
        ArgumentNullException.ThrowIfNull(logicalPlan);
        ArgumentNullException.ThrowIfNull(sourcePlanning);

        var reasonsBySourceId = sourcePlanning.SourcesById.Keys.ToDictionary(
            static sourceContextId => sourceContextId,
            static _ => new HashSet<string>(StringComparer.Ordinal),
            StringComparer.Ordinal);
        var sourceReferences = SourceReferenceIndex.Create(logicalPlan);
        var lifetimePlans = SourceTransferLifetimePlanner.Plan(logicalPlan);

        foreach (var expression in LogicalExpressionTraversal.SelfAndDescendantExpressions(logicalPlan))
        foreach (var methodCall in IrExpressionTraversal.SelfAndDescendants(expression).OfType<MethodCall>())
            RecordDeclaredEntityUsage(methodCall, sourcePlanning, sourceReferences, reasonsBySourceId);

        var plans = new Dictionary<string, SourceTransferUsagePlan>(StringComparer.Ordinal);
        var decisions = new List<PlanningDecision>();
        foreach (var source in sourcePlanning.SourcesById.Values.OrderBy(static source => source.SourceContextId, StringComparer.Ordinal))
        {
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

        return new SourceTransferUsagePlanningResult(plans, decisions);
    }

    private static void RecordDeclaredEntityUsage(
        MethodCall methodCall,
        SourcePlanningFacts sourcePlanning,
        SourceReferenceIndex sourceReferences,
        IReadOnlyDictionary<string, HashSet<string>> reasonsBySourceId)
    {
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
                    AddReason(reference.SourceContextId, reason, reasonsBySourceId);
                return;
            }

            AddReasonToAllSources(
                $"Method '{methodCall.Method.Name}' targets unresolved alias '{methodCall.Alias}', so declared entities are retained conservatively.",
                reasonsBySourceId);
            return;
        }

        var candidates = sourcePlanning.SourcesById.Values
            .Where(source => CanSupplyInjectedSource(source, parameter.ParameterType, sourcePlanning))
            .Select(static source => source.SourceContextId)
            .ToArray();
        if (candidates.Length > 0)
        {
            foreach (var sourceContextId in candidates)
                AddReason(sourceContextId, reason, reasonsBySourceId);
            return;
        }

        if (sourcePlanning.SourceDescriptorsBySourceId.Values.Any(static descriptor => descriptor.RowType == null))
        {
            AddReasonToAllSources(
                $"Method '{methodCall.Method.Name}' has an unaliased source injection with incomplete row metadata.",
                reasonsBySourceId);
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
        IReadOnlyDictionary<string, HashSet<string>> reasonsBySourceId)
    {
        foreach (var reasons in reasonsBySourceId.Values)
            reasons.Add(reason);
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
