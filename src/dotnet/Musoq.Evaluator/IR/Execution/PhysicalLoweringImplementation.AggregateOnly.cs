using System.Collections.Generic;
using System.Globalization;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private TableBuildResult BuildAggregateOnlyTable(
        AggregateOnlyPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        bool scopeAggregateVariables,
        LoweringScope scope)
    {
        var aggregateSource = BuildAggregateSource(
            pipeline.Source.Source,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            CreateSourceRowsScope(resultTableName),
            "aggregate-only",
            scope);
        if (!aggregateSource.IsBuilt)
            return TableBuildResult.Unsupported(aggregateSource.UnsupportedReason);

        var sourceShape = aggregateSource.Source.Shape;
        var source = aggregateSource.Source.Variable;
        var sourceSetup = aggregateSource.Source.Setup;
        var sourceRows = aggregateSource.Source.Rows;
        var outputFields = SelectAggregateOutputFields(pipeline.Project.Fields, pipeline.Bindings);
        if (outputFields == null)
            return TableBuildResult.Unsupported(
                $"Execution IR aggregate-only lowering cannot match {pipeline.Project.Fields.Length.ToString(CultureInfo.InvariantCulture)} projected fields to {pipeline.Bindings.Length.ToString(CultureInfo.InvariantCulture)} aggregate bindings.");

        var resultShape = CreateGeneratedShape(resultShapeName, outputFields);
        var resultTable = new ExecutionVariable(resultTableName, typeof(object));
        var aggregateScopeName = CreateAggregateScopeName(resultTableName, scopeAggregateVariables);
        var finalizationGroupKeys = new AggregateFinalizationGroupKeys([], [], []);
        var rootGroup = CreateAggregateGroupVariable(aggregateScopeName, "rootGroup");
        var currentGroup = CreateAggregateGroupVariable(aggregateScopeName, "group");
        var groups = CreateAggregateGroupVariable(aggregateScopeName, "groupsToFinalize");
        var finalGroup = CreateAggregateGroupVariable(aggregateScopeName, "finalGroup");
        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(sourceShape);
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
            "aggregate-only"));
        if (!aggregateResources.IsBuilt)
            return TableBuildResult.Unsupported(aggregateResources.UnsupportedReason);

        var aggregate = aggregateResources.Value;
        var accumulationBlock = CreateAggregateAccumulationBlock(
            pipeline.Source.Filter,
            sourceShape,
            new ExecutionBlock(
            [
                new ExecutionEnsureAggregateGroup(rootGroup, currentGroup, groups, aggregate.Group.Plan),
                ..aggregate.SetNodes.Nodes,
                ..aggregate.ValueCapture.Nodes
            ]));
        var loop = CreateSourceLoop(sourceShape, sourceRows, source, accumulationBlock);
        var appendRow = CreateAggregateAppendRow(
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
            new ExecutionCreateAggregateContext(rootGroup, currentGroup, groups, aggregate.Group.Plan),
            loop,
            groups,
            finalGroup,
            finalBlock.Value,
            pipeline.PostOperations,
            pipeline.Project.IsDistinct));
    }
}
