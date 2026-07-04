using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static BuildResult<AggregateLoweringResources> CreateAggregateLoweringResources(AggregateLoweringResourceRequest request)
    {
        if (!TryCreateAggregateGroupLowering(
            request.ResultTableName,
            request.FinalizationGroupKeys,
            request.Bindings,
            request.OutputFields,
            request.HavingPredicate,
            request.PostOperations,
            out var aggregateGroup,
            out var aggregateGroupUnsupportedReason))
        {
            return BuildResult<AggregateLoweringResources>.Unsupported(aggregateGroupUnsupportedReason);
        }

        if (!TryCreateAggregateLibraries(
            request.Bindings,
            request.AggregateScopeName,
            out var libraries,
            out var libraryNodes,
            out var unsupportedReason))
        {
            return BuildResult<AggregateLoweringResources>.Unsupported(unsupportedReason);
        }

        var aggregateSetNodes = CreateAggregateSetNodes(
            request.Bindings,
            request.CurrentGroup,
            request.SourceLookup,
            libraries,
            aggregateGroup);
        if (!aggregateSetNodes.Supported)
            return BuildResult<AggregateLoweringResources>.Unsupported(aggregateSetNodes.UnsupportedReason);

        var groupValueCapture = CreateAggregateGroupValueCaptureNodes(
            request.OutputFields,
            request.HavingPredicate,
            request.PostOperations,
            request.FinalizationGroupKeys,
            request.CurrentGroup,
            request.SourceLookup,
            aggregateGroup);
        if (!groupValueCapture.Supported)
            return BuildResult<AggregateLoweringResources>.Unsupported(groupValueCapture.UnsupportedReason);

        var finalizationContext = CreateAggregateFinalizationContext(
            request.FinalGroup,
            request.FinalizationGroupKeys,
            request.Bindings,
            groupValueCapture.CapturedValues,
            aggregateSetNodes.TypedAccumulators,
            aggregateGroup.Shape,
            request.AggregateKind);

        return BuildResult<AggregateLoweringResources>.Success(new AggregateLoweringResources(
            aggregateGroup,
            libraryNodes,
            aggregateSetNodes,
            groupValueCapture,
            finalizationContext));
    }
}
