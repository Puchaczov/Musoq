namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private TableBuildResult BuildSingleKeyAggregateTableCore(
        AggregateSingleKeyPipeline pipeline,
        SingleKeyAggregateExecutionSource source,
        string resultTableName,
        string resultShapeName,
        bool scopeAggregateVariables
    )
    {
        var groupKey = ExecutionExpressionConverter.Convert(pipeline.GroupKey, source.Lookup);
        if (!pipeline.Aggregate.InitializeConstantGroup && CanUseLeanDistinct(pipeline))
        {
            return BuildLeanDistinctTable(
                pipeline.Project.Fields,
                pipeline.PostOperations,
                pipeline.Source.Filter,
                source,
                groupKey,
                resultTableName,
                resultShapeName,
                scopeAggregateVariables
            );
        }

        var projectionAttempt = CreateAggregatePostOperationProjection(
            resultTableName,
            resultShapeName,
            NormalizeProjectedFieldIndexes(pipeline.Project.Fields),
            pipeline.PostOperations,
            source.Lookup
        );
        if (!projectionAttempt.IsBuilt)
            return TableBuildResult.Unsupported(projectionAttempt.UnsupportedReason);

        var projection = projectionAttempt.Value;
        var outputFields = projection.MaterializedFields;
        var resultShape = projection.WorkingShape;
        var resultTable = projection.WorkingTable;
        var aggregateScopeName = CreateAggregateScopeName(resultTableName, scopeAggregateVariables);
        var finalizationGroupKeys = new AggregateFinalizationGroupKeys(
            [pipeline.GroupKey],
            [pipeline.GroupKeyName],
            [pipeline.GroupKeyType]
        );
        var rootGroup = CreateAggregateGroupVariable(aggregateScopeName, "rootGroup");
        var groupsToFinalize = CreateAggregateGroupVariable(aggregateScopeName, "groupsToFinalize");
        var groups = CreateAggregateVariable(aggregateScopeName, "groups", typeof(object));
        var nullGroup =
            pipeline.GroupKeyType.IsValueType && Nullable.GetUnderlyingType(pipeline.GroupKeyType) is null
                ? null
                : CreateAggregateGroupVariable(aggregateScopeName, "nullGroup");
        var currentGroup = CreateAggregateGroupVariable(aggregateScopeName, "group");
        var finalGroup = CreateAggregateGroupVariable(aggregateScopeName, "finalGroup");
        var aggregateResources = CreateAggregateLoweringResources(
            new AggregateLoweringResourceRequest(
                resultTableName,
                aggregateScopeName,
                pipeline.Bindings,
                outputFields,
                pipeline.HavingPredicate,
                projection.PostOperations,
                finalizationGroupKeys,
                currentGroup,
                finalGroup,
                source.Lookup,
                "single-key aggregate"
            )
        );
        if (!aggregateResources.IsBuilt)
            return TableBuildResult.Unsupported(aggregateResources.UnsupportedReason);

        var aggregate = aggregateResources.Value;
        var aggregateBody = new ExecutionBlock([.. aggregate.SetNodes.Nodes, .. aggregate.ValueCapture.Nodes]);
        var getGroup = new ExecutionGetOrAddSingleKeyAggregateGroup(
            rootGroup,
            groups,
            groupsToFinalize,
            currentGroup,
            groupKey,
            pipeline.GroupKeyName,
            pipeline.GroupKeyType,
            nullGroup,
            aggregate.Group.Plan
        );
        var initializeGroup = pipeline.Aggregate.InitializeConstantGroup;
        var serialAggregateBody = initializeGroup
            ? aggregateBody
            : new ExecutionBlock([getGroup, .. aggregateBody.Nodes]);
        var accumulationBlock = CreateAggregateAccumulationBlock(
            pipeline.Source.Filter,
            source.Lookup,
            serialAggregateBody
        );
        var loop = source.CreateLoop(accumulationBlock);
        ExecutionNode aggregateLoop = initializeGroup
            ? loop
            : TryCreateParallelSingleKeyAggregateLoop(
                pipeline,
                source,
                groupKey,
                currentGroup,
                rootGroup,
                groupsToFinalize,
                aggregateBody,
                aggregate.Group.Shape,
                aggregate.SetNodes
            ) ?? (ExecutionNode)loop;
        var appendRow = CreateGroupedAggregateAppendRow(
            resultTable,
            resultShape,
            outputFields,
            aggregate.FinalizationContext
        );

        if (!appendRow.IsBuilt)
            return TableBuildResult.Unsupported(appendRow.UnsupportedReason);

        var finalBlock = CreateAggregateFinalBlock(
            pipeline.HavingPredicate,
            aggregate.FinalizationContext,
            appendRow.Value
        );
        if (!finalBlock.IsBuilt)
            return TableBuildResult.Unsupported(finalBlock.UnsupportedReason);

        return CompleteAggregateTableBuild(
            new AggregateTableCompletion(
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
                    aggregate.Group.Plan
                ),
                aggregateLoop,
                groupsToFinalize,
                finalGroup,
                finalBlock.Value,
                projection.PostOperations,
                pipeline.Project.IsDistinct,
                projection.FinalProjection,
                initializeGroup ? getGroup : null
            )
        );
    }
}
