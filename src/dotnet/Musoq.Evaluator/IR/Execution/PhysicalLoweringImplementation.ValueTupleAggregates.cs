using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private TableBuildResult BuildValueTupleAggregateTable(
        ValueTupleAggregatePipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        bool scopeAggregateVariables,
        LoweringScope scope)
    {
        if (pipeline.GroupKeys.Length <= 1)
            return TableBuildResult.Unsupported(
                $"Execution IR value-tuple aggregate lowering supports at least 2 group keys. Found {pipeline.GroupKeys.Length.ToString(CultureInfo.InvariantCulture)} keys.");

        var aggregateSource = BuildAggregateSource(
            pipeline.Source.Source,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            CreateSourceRowsScope(resultTableName),
            "value-tuple aggregate",
            scope);
        if (!aggregateSource.IsBuilt)
            return TableBuildResult.Unsupported(aggregateSource.UnsupportedReason);

        var sourceShape = aggregateSource.Source.Shape;
        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(sourceShape);
        var groupKeys = pipeline.GroupKeys
            .Select(key => ExecutionExpressionConverter.Convert(key, sourceLookup))
            .ToArray();
        if (CanUseLeanDistinct(pipeline))
        {
            var leanDistinctSource = new SingleKeyAggregateExecutionSource(
                sourceLookup,
                aggregateSource.Source.Shapes,
                aggregateSource.Source.Setup,
                body => CreateSourceLoop(sourceShape, aggregateSource.Source.Rows, aggregateSource.Source.Variable, body));
            var distinctKey = new ExecutionValueTupleKey(
                groupKeys,
                CreateValueTupleType(groupKeys.Select(static key => key.ReturnType.ResolveClrType()).ToArray()));

            return BuildLeanDistinctTable(
                pipeline.Project.Fields,
                pipeline.PostOperations,
                pipeline.Source.Filter,
                leanDistinctSource,
                distinctKey,
                resultTableName,
                resultShapeName,
                scopeAggregateVariables);
        }

        var aggregateScopeName = CreateAggregateScopeName(resultTableName, scopeAggregateVariables);
        var currentGroup = CreateAggregateGroupVariable(aggregateScopeName, "group");
        var outputFields = NormalizeProjectedFieldIndexes(pipeline.Project.Fields);
        var resultShape = CreateGeneratedShape(resultShapeName, outputFields);
        var resultTable = new ExecutionVariable(resultTableName, typeof(object));
        var rootGroup = CreateAggregateGroupVariable(aggregateScopeName, "rootGroup");
        var groupsToFinalize = CreateAggregateGroupVariable(aggregateScopeName, "groupsToFinalize");
        var finalGroup = CreateAggregateGroupVariable(aggregateScopeName, "finalGroup");
        var finalizationGroupKeys = new AggregateFinalizationGroupKeys(pipeline.GroupKeys, pipeline.GroupKeyNames, pipeline.GroupKeyTypes);
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
            sourceLookup,
            "value-tuple aggregate"));
        if (!aggregateResources.IsBuilt)
            return TableBuildResult.Unsupported(aggregateResources.UnsupportedReason);

        var aggregate = aggregateResources.Value;
        var groupDictionaries = CreateValueTupleGroupDictionaries(aggregateScopeName, aggregate.Group.Plan);

        var accumulationBlock = CreateAggregateAccumulationBlock(
            pipeline.Source.Filter,
            sourceShape,
            new ExecutionBlock(
            [
                new ExecutionGetOrAddValueTupleAggregateGroup(
                    rootGroup,
                    groupDictionaries,
                    groupsToFinalize,
                    currentGroup,
                    groupKeys,
                    pipeline.GroupKeyNames,
                    pipeline.GroupKeyTypes,
                    aggregate.Group.Plan),
                ..aggregate.SetNodes.Nodes,
                ..aggregate.ValueCapture.Nodes
            ]));
        var source = aggregateSource.Source.Variable;
        var sourceSetup = aggregateSource.Source.Setup;
        var sourceRows = aggregateSource.Source.Rows;
        var loop = CreateSourceLoop(sourceShape, sourceRows, source, accumulationBlock);
        var appendRow = CreateGroupedAggregateAppendRow(
            resultTable,
            resultShape,
            outputFields,
            aggregate.FinalizationContext);

        if (!appendRow.IsBuilt)
            return TableBuildResult.Unsupported(appendRow.UnsupportedReason);

        var finalBlock = CreateAggregateFinalBlock(pipeline.HavingPredicate, aggregate.FinalizationContext, appendRow.Value);
        if (!finalBlock.IsBuilt)
            return TableBuildResult.Unsupported(finalBlock.UnsupportedReason);

        return CompleteAggregateTableBuild(new AggregateTableCompletion(
            aggregateSource.Source.Shapes,
            sourceSetup,
            aggregate,
            resultTable,
            resultShape,
            new ExecutionCreateValueTupleAggregateContext(
                rootGroup,
                groupDictionaries,
                groupsToFinalize,
                pipeline.GroupKeyTypes,
                aggregate.Group.Plan),
            loop,
            groupsToFinalize,
            finalGroup,
            finalBlock.Value,
            pipeline.PostOperations,
            pipeline.Project.IsDistinct));
    }
}
