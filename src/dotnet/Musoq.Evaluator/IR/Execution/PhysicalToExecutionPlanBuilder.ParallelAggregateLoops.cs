using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private ExecutionParallelSingleKeyAggregateLoop? TryCreateParallelSingleKeyAggregateLoop(
        SingleKeyAggregatePipeline pipeline,
        SingleKeyAggregateExecutionSource source,
        ExecutionExpression groupKey,
        ExecutionVariable currentGroup,
        ExecutionVariable rootGroup,
        ExecutionVariable groupsToFinalize,
        ExecutionBlock aggregateBody,
        AggregateGroupShape aggregateGroupShape,
        AggregateSetBuildResult aggregateSetNodes)
    {
        if (!ExecutionStrategies.CanUseParallelSingleKeyAggregate(pipeline.Aggregate) ||
            pipeline.Source.Filter != null ||
            source.ParallelSource == null ||
            source.ParallelRows == null ||
            !CanUseParallelAggregateSource(source.Lookup) ||
            !CanUseParallelAggregateRows(source.ParallelRows) ||
            !CanUseParallelAggregateSets(aggregateSetNodes) ||
            aggregateGroupShape.RequiresParentLinks ||
            !CanMergeAggregateGroupShape(aggregateGroupShape))
        {
            return null;
        }

        var maxDegreeOfParallelism = CompilationParallelism.ResolveMaxDegreeOfParallelism(_compilationOptions);
        if (maxDegreeOfParallelism <= 1)
            return null;

        return new ExecutionParallelSingleKeyAggregateLoop(
            source.ParallelSource,
            source.ParallelRows,
            groupKey,
            pipeline.GroupKeyName,
            pipeline.GroupKeyType,
            rootGroup,
            groupsToFinalize,
            currentGroup,
            aggregateBody,
            aggregateGroupShape,
            ParallelAggregateRowThreshold,
            ParallelAggregateCardinalitySampleSize,
            ParallelAggregateMaxDistinctSample,
            maxDegreeOfParallelism);
    }

    private static bool CanUseParallelAggregateSource(IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        return sourceLookup.Values.All(static shape => shape is not ExpandoAdapterShape);
    }

    private static bool CanUseParallelAggregateRows(ExecutionExpression rows)
    {
        return ParallelExecutionEligibilityRules.CanUseParallelRows(rows);
    }

    private ExecutionParallelFilterProjectLoop? TryCreateParallelFilterProjectLoop(
        SupportedPipeline pipeline,
        RowShape sourceShape,
        ExecutionVariable source,
        ExecutionExpression sourceRows,
        ExecutionSourceLoop serialLoop,
        ExecutionAppendRow appendRow)
    {
        if (!ExecutionStrategies.CanUseParallelFilterProject(pipeline.Project) ||
            pipeline.PostOperations.Count != 0 ||
            pipeline.Project.IsDistinct ||
            pipeline.Source is not (PhysicalSchemaScanNode or PhysicalCteRefNode) ||
            sourceShape is ExpandoAdapterShape ||
            !CanUseParallelFilterProjectRows(sourceRows) ||
            appendRow.AppendMode != ExecutionAppendMode.Direct)
        {
            return null;
        }

        var predicate = pipeline.Filter == null
            ? null
            : ExecutionExpressionConverter.Convert(pipeline.Filter.Predicate, sourceShape);

        if (predicate is ExecutionRawExpression ||
            !CanUseParallelFilterProjectExpression(predicate) ||
            !CanUseParallelFilterProjectAppend(appendRow) ||
            !HasParallelWorthyMethodCall(predicate, appendRow))
        {
            return null;
        }

        var maxDegreeOfParallelism = CompilationParallelism.ResolveMaxDegreeOfParallelism(_compilationOptions);
        if (maxDegreeOfParallelism <= 1)
            return null;

        return new ExecutionParallelFilterProjectLoop(
            source,
            sourceRows,
            predicate,
            appendRow,
            serialLoop.Body,
            ParallelFilterProjectRowThreshold,
            maxDegreeOfParallelism);
    }

    private static bool CanUseParallelFilterProjectRows(ExecutionExpression rows)
    {
        return ParallelExecutionEligibilityRules.CanUseParallelRows(rows);
    }

    private static bool CanUseParallelFilterProjectAppend(ExecutionAppendRow appendRow)
    {
        return appendRow.Values.All(static value => CanUseParallelFilterProjectExpression(value.Value)) &&
               appendRow.Contexts.All(CanUseParallelFilterProjectExpression) &&
               (appendRow.ContextLayout == null ||
                appendRow.ContextLayout.Segments.All(static segment =>
                    CanUseParallelFilterProjectExpression(segment.Value)));
    }

    private static bool HasParallelWorthyMethodCall(ExecutionExpression? predicate, ExecutionAppendRow appendRow)
    {
        return ContainsMethodCall(predicate) ||
               appendRow.Values.Any(static value => ContainsMethodCall(value.Value)) ||
               appendRow.Contexts.Any(ContainsMethodCall) ||
               (appendRow.ContextLayout != null &&
                appendRow.ContextLayout.Segments.Any(static segment => ContainsMethodCall(segment.Value)));
    }

    private static bool ContainsMethodCall(ExecutionExpression? expression)
    {
        return ParallelExecutionEligibilityRules.ContainsMethodCall(expression);
    }

    private static bool CanUseParallelFilterProjectExpression(ExecutionExpression? expression)
    {
        return ParallelExecutionEligibilityRules.CanUseFilterProjectExpression(expression).IsEligible;
    }

    private static bool CanUseParallelAggregateSets(AggregateSetBuildResult aggregateSetNodes)
    {
        if (aggregateSetNodes.Nodes.Count == 0)
            return false;

        return aggregateSetNodes.Nodes.All(static node => node is ExecutionAggregateSet);
    }

    private static bool CanMergeAggregateGroupShape(AggregateGroupShape shape)
    {
        return shape is { RequiresParentLinks: false, Accumulators.Count: > 0 } &&
               shape.Accumulators.All(static accumulator => accumulator.CanMerge);
    }

}
