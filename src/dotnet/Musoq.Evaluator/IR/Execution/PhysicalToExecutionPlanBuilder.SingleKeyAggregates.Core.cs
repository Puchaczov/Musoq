namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult BuildSingleKeyAggregateTableCore(
        SingleKeyAggregatePipeline pipeline,
        SingleKeyAggregateExecutionSource source,
        string resultTableName,
        string resultShapeName,
        bool scopeAggregateVariables)
    {
        var groupKey = ExecutionExpressionConverter.Convert(pipeline.GroupKey, source.Lookup);
        if (groupKey is ExecutionRawExpression)
            return TableBuildResult.Unsupported(
                $"Execution IR single-key aggregate lowering cannot convert group key expression {pipeline.GroupKey.GetType().Name}.");

        if (CanUseLeanDistinct(pipeline))
        {
            return BuildLeanDistinctTable(
                pipeline.Project.Fields,
                pipeline.PostOperations,
                pipeline.Source.Filter,
                source,
                groupKey,
                resultTableName,
                resultShapeName,
                scopeAggregateVariables);
        }

        var outputFields = NormalizeProjectedFieldIndexes(pipeline.Project.Fields);
        var resultShape = CreateGeneratedShape(resultShapeName, outputFields);
        var resultTable = new ExecutionVariable(resultTableName, typeof(object));
        var aggregateScopeName = CreateAggregateScopeName(resultTableName, scopeAggregateVariables);
        var finalizationGroupKeys = new AggregateFinalizationGroupKeys([pipeline.GroupKey], [pipeline.GroupKeyName], [pipeline.GroupKeyType]);
        var rootGroup = CreateAggregateGroupVariable(aggregateScopeName, "rootGroup");
        var groupsToFinalize = CreateAggregateGroupVariable(aggregateScopeName, "groupsToFinalize");
        var groups = CreateAggregateVariable(
            aggregateScopeName,
            "groups",
            typeof(object));
        var nullGroup = pipeline.GroupKeyType.IsValueType
            ? null
            : CreateAggregateGroupVariable(aggregateScopeName, "nullGroup");
        var currentGroup = CreateAggregateGroupVariable(aggregateScopeName, "group");
        var finalGroup = CreateAggregateGroupVariable(aggregateScopeName, "finalGroup");
        var aggregateResources = CreateAggregateLoweringResources(new AggregateLoweringResourceRequest(
            resultTableName,
            aggregateScopeName,
            pipeline.Bindings,
            outputFields,
            pipeline.HavingPredicate,
            pipeline.PostOperations,
            finalizationGroupKeys,
            currentGroup,
            finalGroup,
            source.Lookup,
            "single-key aggregate"));
        if (!aggregateResources.Supported)
            return TableBuildResult.Unsupported(aggregateResources.UnsupportedReason);

        var aggregate = aggregateResources.Value;
        var aggregateBody = new ExecutionBlock([..aggregate.SetNodes.Nodes, ..aggregate.ValueCapture.Nodes]);
        var serialAggregateBody = new ExecutionBlock(
        [
            new ExecutionGetOrAddSingleKeyAggregateGroup(
                rootGroup,
                groups,
                groupsToFinalize,
                currentGroup,
                groupKey,
                pipeline.GroupKeyName,
                pipeline.GroupKeyType,
                nullGroup,
                aggregate.Group.Plan),
            ..aggregateBody.Nodes
        ]);
        var accumulationBlock = CreateAggregateAccumulationBlock(
            pipeline.Source.Filter,
            source.Lookup,
            serialAggregateBody);
        var loop = source.CreateLoop(accumulationBlock);
        ExecutionNode aggregateLoop = TryCreateParallelSingleKeyAggregateLoop(
            pipeline,
            source,
            groupKey,
            currentGroup,
            rootGroup,
            groupsToFinalize,
            aggregateBody,
            aggregate.Group.Shape,
            aggregate.SetNodes)
            ?? (ExecutionNode)loop;
        var appendRow = CreateGroupedAggregateAppendRow(
            resultTable,
            resultShape,
            outputFields,
            aggregate.FinalizationContext);

        if (!appendRow.Supported)
            return TableBuildResult.Unsupported(appendRow.UnsupportedReason);

        var finalBlock = CreateAggregateFinalBlock(pipeline.HavingPredicate, aggregate.FinalizationContext, appendRow.Value);
        if (!finalBlock.Supported)
            return TableBuildResult.Unsupported(finalBlock.UnsupportedReason);

        return CompleteAggregateTableBuild(new AggregateTableCompletion(
            source.Shapes,
            source.Setup,
            aggregate,
            resultTable,
            resultShape,
            new ExecutionCreateSingleKeyAggregateContext(
                rootGroup,
                groups,
                groupsToFinalize,
                nullGroup,
                pipeline.GroupKeyType,
                aggregate.Group.Plan),
            aggregateLoop,
            groupsToFinalize,
            finalGroup,
            finalBlock.Value,
            pipeline.PostOperations,
            pipeline.Project.IsDistinct));
    }
}
