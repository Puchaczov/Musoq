using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed partial class ParallelStrategyPlanner
{
    private ParallelPlanEligibility EvaluateParallelAggregate(SingleKeyAggregatePipeline pipeline)
    {
        if (compilationOptions.ParallelizationMode != ParallelizationMode.Full)
            return ParallelPlanEligibility.Disabled("Compilation option does not request full parallel execution.");

        if (pipeline.SourceFilter != null)
            return ParallelPlanEligibility.Skipped("Source filter is present, so parallel single-key aggregate cannot preserve current aggregate filtering semantics.");

        var sourceShapeResolution = ResolveParallelSourceShape(pipeline.Source);
        if (sourceShapeResolution.SourceShape is not { } sourceShape)
            return ParallelPlanEligibility.Skipped(sourceShapeResolution.Reason);

        if (sourceShape is ExpandoAdapterShape)
            return ParallelPlanEligibility.Skipped("Source shape is dynamic, so parallel single-key aggregate cannot use stable row access.");

        if (!CanUseParallelSourceRows(pipeline.Source))
            return ParallelPlanEligibility.Skipped($"Unsupported row source {pipeline.Source.GetType().Name}; parallel single-key aggregate requires enumerable or stored-table rows.");

        var aggregateBindingsEligibility = CanUseParallelAggregateBindings(pipeline.Aggregate.Bindings);
        if (!aggregateBindingsEligibility.IsEligible)
            return ParallelPlanEligibility.Skipped(aggregateBindingsEligibility.Reason);

        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(sourceShape);
        var groupKey = ExecutionExpressionConverter.Convert(pipeline.Aggregate.GroupKey, sourceLookup);
        var groupKeyEligibility = ParallelExecutionEligibilityRules.CanUseAggregateGroupKeyExpression(groupKey);
        if (!groupKeyEligibility.IsEligible)
            return ParallelPlanEligibility.Skipped($"Group key is not parallel-safe: {groupKeyEligibility.Reason}");

        if (CompilationParallelism.ResolveMaxDegreeOfParallelism(compilationOptions) <= 1)
            return ParallelPlanEligibility.Skipped("Insufficient parallelism is available on this machine.");

        return ParallelPlanEligibility.Enabled("Planner proved source rows, group key, and aggregate kernels are safe for parallel single-key aggregation.");
    }

    private ParallelPlanEligibility EvaluateParallelFilterProject(SupportedPipeline pipeline)
    {
        if (compilationOptions.ParallelizationMode != ParallelizationMode.Full)
            return ParallelPlanEligibility.Disabled("Compilation option does not request full parallel execution.");

        if (pipeline.PostOperations.Count != 0)
            return ParallelPlanEligibility.Skipped("Post-operations are present, so filter/project remains serial before ordered or paged materialization.");

        if (pipeline.Project.IsDistinct)
            return ParallelPlanEligibility.Skipped("Distinct projection requires result materialization before de-duplication.");

        if (pipeline.Source is not (PhysicalSchemaScanNode or PhysicalCteRefNode))
            return ParallelPlanEligibility.Skipped($"Unsupported row source {pipeline.Source.GetType().Name}; parallel filter/project supports schema scans and CTE rows only.");

        var sourceShapeResolution = ResolveParallelSourceShape(pipeline.Source);
        if (sourceShapeResolution.SourceShape is not { } sourceShape)
            return ParallelPlanEligibility.Skipped(sourceShapeResolution.Reason);

        if (sourceShape is ExpandoAdapterShape)
            return ParallelPlanEligibility.Skipped("Source shape is dynamic, so parallel filter/project cannot use stable field access.");

        if (!CanUseParallelSourceRows(pipeline.Source))
            return ParallelPlanEligibility.Skipped($"Unsupported row source {pipeline.Source.GetType().Name}; parallel filter/project requires enumerable or stored-table rows.");

        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(sourceShape);
        var predicateEligibility = CanUseParallelFilterProjectPredicate(pipeline.Filter, sourceShape);
        if (!predicateEligibility.IsEligible)
            return ParallelPlanEligibility.Skipped(predicateEligibility.Reason);

        var fieldsEligibility = CanUseParallelFilterProjectFields(pipeline.Project.Fields, sourceLookup);
        if (!fieldsEligibility.IsEligible)
            return ParallelPlanEligibility.Skipped(fieldsEligibility.Reason);

        if (!HasParallelWorthyMethodCall(pipeline))
            return ParallelPlanEligibility.Skipped("Projection and predicate contain no method-heavy expression worth parallelizing.");

        if (CompilationParallelism.ResolveMaxDegreeOfParallelism(compilationOptions) <= 1)
            return ParallelPlanEligibility.Skipped("Insufficient parallelism is available on this machine.");

        return ParallelPlanEligibility.Enabled("Planner proved source rows, expressions, and append shape are safe for parallel filter/project lowering.");
    }
}
