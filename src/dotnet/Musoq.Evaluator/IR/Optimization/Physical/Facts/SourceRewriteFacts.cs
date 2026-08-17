using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using PlanProperties = Musoq.Evaluator.IR.Planning.PlanProperties;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal sealed record SourceRewriteFacts(
    IReadOnlyDictionary<string, IrExpression[]> PushedPredicatesBySourceId,
    IReadOnlyDictionary<string, string[]> ProjectedColumnsBySourceId,
    IReadOnlyDictionary<string, SourcePredicatePlan> SourcePredicatePlansBySourceId,
    IReadOnlyDictionary<string, SourcePlanResult> SourcePlanResultsBySourceId)
{
    public static SourceRewriteFacts From(PlanProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        return new SourceRewriteFacts(
            properties.SourcePlanning.PushedPredicatesBySourceId,
            properties.SourcePlanning.ProjectedColumnsBySourceId,
            properties.SourcePlanning.SourcePredicatePlansBySourceId,
            properties.SourcePlanning.SourcePlanResultsBySourceId);
    }

    public SourceRewriteFacts WithSourcePlanResults(
        IReadOnlyDictionary<string, SourcePlanResult> sourcePlanResultsBySourceId)
    {
        ArgumentNullException.ThrowIfNull(sourcePlanResultsBySourceId);

        return this with
        {
            SourcePlanResultsBySourceId = sourcePlanResultsBySourceId
        };
    }

    public PlanProperties ApplyTo(PlanProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        return properties with
        {
            PushedPredicatesBySourceId = PushedPredicatesBySourceId,
            ProjectedColumnsBySourceId = ProjectedColumnsBySourceId,
            SourcePredicatePlansBySourceId = SourcePredicatePlansBySourceId,
            SourcePlanResultsBySourceId = SourcePlanResultsBySourceId
        };
    }
}

