namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static LoweringAttempt<AggregateLoweringResources> CreateAggregateLoweringResources(AggregateLoweringResourceRequest request)
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
            return LoweringAttempt<AggregateLoweringResources>.Unsupported(aggregateGroupUnsupportedReason);
        }

        if (!TryCreateAggregateLibraries(
            request.Bindings,
            request.AggregateScopeName,
            out var libraries,
            out var libraryNodes,
            out var unsupportedReason))
        {
            return LoweringAttempt<AggregateLoweringResources>.Unsupported(unsupportedReason);
        }

        var aggregateSetNodes = CreateAggregateSetNodes(
            request.Bindings,
            request.CurrentGroup,
            request.SourceLookup,
            libraries,
            aggregateGroup);
        if (!aggregateSetNodes.IsBuilt)
            return LoweringAttempt<AggregateLoweringResources>.Unsupported(aggregateSetNodes.UnsupportedReason);

        var groupValueCapture = CreateAggregateGroupValueCaptureNodes(
            request.OutputFields,
            request.HavingPredicate,
            request.PostOperations,
            request.FinalizationGroupKeys,
            request.CurrentGroup,
            request.SourceLookup,
            aggregateGroup);
        if (!groupValueCapture.IsBuilt)
            return LoweringAttempt<AggregateLoweringResources>.Unsupported(groupValueCapture.UnsupportedReason);

        var finalizationContext = CreateAggregateFinalizationContext(
            request.FinalGroup,
            request.FinalizationGroupKeys,
            request.Bindings,
            groupValueCapture.CapturedValues,
            aggregateSetNodes.TypedAccumulators,
            aggregateGroup.Shape,
            request.AggregateKind);

        return LoweringAttempt<AggregateLoweringResources>.Built(new AggregateLoweringResources(
            aggregateGroup,
            libraryNodes,
            aggregateSetNodes,
            groupValueCapture,
            finalizationContext));
    }
}
