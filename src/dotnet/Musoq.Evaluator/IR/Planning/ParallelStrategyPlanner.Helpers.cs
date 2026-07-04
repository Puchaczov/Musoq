using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed partial class ParallelStrategyPlanner
{
    private static PlanningDecision CreateParallelDecision(
        string ruleName,
        string target,
        string outcome,
        PlanningConfidence confidence,
        string reason)
    {
        return new PlanningDecision(
            PlanningDecisionCategory.ParallelEligibility,
            ruleName,
            target,
            outcome,
            confidence,
            reason);
    }

    private static ParallelEligibilityCheck CanUseParallelAggregateBindings(AggregateBinding[] bindings)
    {
        if (bindings.Length == 0)
            return ParallelEligibilityCheck.Skipped("No aggregate set operations are present for mergeable parallel aggregation.");

        if (bindings.Any(static binding => binding.ParentDepth > 0))
            return ParallelEligibilityCheck.Skipped("Aggregate binding requires parent group links, which cannot be merged in the parallel aggregate loop.");

        if (bindings.Any(static binding => binding.Kernel == null))
            return ParallelEligibilityCheck.Skipped("At least one aggregate binding has no typed aggregate kernel for parallel merging.");

        if (bindings.Any(static binding => binding.Kernel is { SupportsMerge: false }))
            return ParallelEligibilityCheck.Skipped("At least one aggregate binding is not mergeable.");

        return ParallelEligibilityCheck.Enabled;
    }

    private static ParallelEligibilityCheck CanUseParallelFilterProjectPredicate(
        PhysicalFilterNode? filter,
        PlanningRowShape sourceShape)
    {
        if (filter == null)
            return ParallelEligibilityCheck.Enabled;

        var predicateEligibility = ParallelPlanningEligibilityRules.CanUseFilterProjectExpression(filter.Predicate, sourceShape);
        if (predicateEligibility.IsEligible)
            return ParallelEligibilityCheck.Enabled;

        return ParallelEligibilityCheck.Skipped($"Predicate is not parallel-safe: {predicateEligibility.Reason}");
    }

    private static ParallelEligibilityCheck CanUseParallelFilterProjectFields(
        IReadOnlyList<ProjectedField> fields,
        PlanningRowShape sourceShape)
    {
        foreach (var field in fields)
        {
            var fieldEligibility = ParallelPlanningEligibilityRules.CanUseFilterProjectExpression(field.Expression, sourceShape);
            if (fieldEligibility.IsEligible)
                continue;

            return ParallelEligibilityCheck.Skipped($"Projected field {field.OutputName} is not parallel-safe: {fieldEligibility.Reason}");
        }

        return ParallelEligibilityCheck.Enabled;
    }

    private ParallelSourceShapeResolution ResolveParallelSourceShape(PhysicalNode source)
    {
        var resolution = source switch
        {
            PhysicalSchemaScanNode scan => ParallelSourceShapeResolution.Resolved(shapeResolver.ResolveSourceShape(scan)),
            PhysicalCteRefNode cteRef => ParallelSourceShapeResolution.Resolved(shapeResolver.ResolveCteRefShape(cteRef)),
            PhysicalInterpretSourceNode interpret => ParallelSourceShapeResolution.Resolved(shapeResolver.ResolveInterpretSourceShape(interpret)),
            PhysicalPropertySourceNode property => ParallelSourceShapeResolution.Resolved(shapeResolver.ResolvePropertySourceShape(property)),
            PhysicalAccessMethodSourceNode accessMethod => ParallelSourceShapeResolution.Resolved(shapeResolver.ResolveAccessMethodSourceShape(accessMethod)),
            _ => null!
        };

        if (resolution != null)
            return resolution;

        return ParallelSourceShapeResolution.Unresolved(
            $"Unsupported row source {source.GetType().Name}; planner cannot resolve a stable source row shape.");
    }

    private static bool CanUseParallelSourceRows(PhysicalNode source)
    {
        return source is PhysicalSchemaScanNode or PhysicalCteRefNode;
    }

    private static bool HasParallelWorthyMethodCall(SupportedPipeline pipeline)
    {
        return (pipeline.Filter != null && ParallelPlanningEligibilityRules.ContainsMethodCall(pipeline.Filter.Predicate)) ||
               pipeline.Project.Fields.Any(static field => ParallelPlanningEligibilityRules.ContainsMethodCall(field.Expression));
    }

    private static int ResolveMaxDegreeOfParallelism(int taskCount)
    {
        return Math.Max(1, taskCount);
    }
}
