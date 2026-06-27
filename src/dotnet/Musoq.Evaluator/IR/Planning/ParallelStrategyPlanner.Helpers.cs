using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
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
        RowShape sourceShape)
    {
        if (filter == null)
            return ParallelEligibilityCheck.Enabled;

        var predicate = ExecutionExpressionConverter.Convert(filter.Predicate, sourceShape);
        var predicateEligibility = ParallelExecutionEligibilityRules.CanUseFilterProjectExpression(predicate);
        if (predicateEligibility.IsEligible)
            return ParallelEligibilityCheck.Enabled;

        return ParallelEligibilityCheck.Skipped($"Predicate is not parallel-safe: {predicateEligibility.Reason}");
    }

    private static ParallelEligibilityCheck CanUseParallelFilterProjectFields(
        IReadOnlyList<ProjectedField> fields,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        foreach (var field in fields)
        {
            var expression = ExecutionExpressionConverter.Convert(field.Expression, sourceLookup);
            var fieldEligibility = ParallelExecutionEligibilityRules.CanUseFilterProjectExpression(expression);
            if (fieldEligibility.IsEligible)
                continue;

            return ParallelEligibilityCheck.Skipped($"Projected field {field.OutputName} is not parallel-safe: {fieldEligibility.Reason}");
        }

        return ParallelEligibilityCheck.Enabled;
    }

    private ParallelSourceShapeResolution ResolveParallelSourceShape(PhysicalNode source)
    {
        var sourceShape = source switch
        {
            PhysicalSchemaScanNode scan => shapeResolver.ResolveSourceShape(scan),
            PhysicalCteRefNode cteRef => ExecutionStrategyPipelineDecomposer.CreateTableRowShape(cteRef),
            PhysicalInterpretSourceNode interpret => shapeResolver.ResolveInterpretSourceShape(interpret),
            PhysicalPropertySourceNode property => shapeResolver.ResolvePropertySourceShape(property),
            PhysicalAccessMethodSourceNode accessMethod => shapeResolver.ResolveAccessMethodSourceShape(accessMethod),
            _ => null!
        };

        if (sourceShape != null)
            return ParallelSourceShapeResolution.Resolved(sourceShape);

        return ParallelSourceShapeResolution.Unresolved(
            $"Unsupported row source {source.GetType().Name}; planner cannot resolve a stable source row shape.");
    }

    private static bool CanUseParallelSourceRows(PhysicalNode source)
    {
        return source is PhysicalSchemaScanNode or PhysicalCteRefNode;
    }

    private static bool HasParallelWorthyMethodCall(SupportedPipeline pipeline)
    {
        return (pipeline.Filter != null && ParallelExecutionEligibilityRules.ContainsMethodCall(pipeline.Filter.Predicate)) ||
               pipeline.Project.Fields.Any(static field => ParallelExecutionEligibilityRules.ContainsMethodCall(field.Expression));
    }

    private static int ResolveMaxDegreeOfParallelism(int taskCount)
    {
        return Math.Max(1, taskCount);
    }
}
