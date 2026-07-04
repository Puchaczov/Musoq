using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private AggregateLoweringCoordinator CreateAggregateLoweringCoordinator()
    {
        return new AggregateLoweringCoordinator(
            BuildAggregateOnlyPipeline,
            BuildSingleKeyAggregatePipeline,
            BuildValueTupleAggregatePipeline,
            BuildAggregateOnlyTable,
            BuildSingleKeyAggregateTable,
            BuildValueTupleAggregateTable);
    }

    private delegate ExecutionPlanBuildResult BuildAggregateOnlyPlanDelegate(
        AggregateOnlyPipeline pipeline,
        string identifier,
        PhysicalToExecutionLoweringSession session);

    private delegate ExecutionPlanBuildResult BuildSingleKeyAggregatePlanDelegate(
        SingleKeyAggregatePipeline pipeline,
        string identifier,
        PhysicalToExecutionLoweringSession session);

    private delegate ExecutionPlanBuildResult BuildValueTupleAggregatePlanDelegate(
        ValueTupleAggregatePipeline pipeline,
        string identifier,
        PhysicalToExecutionLoweringSession session);

    private delegate TableBuildResult BuildAggregateOnlyTableDelegate(
        AggregateOnlyPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        bool scopeAggregateVariables,
        PhysicalToExecutionLoweringSession session);

    private delegate TableBuildResult BuildSingleKeyAggregateTableDelegate(
        SingleKeyAggregatePipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        bool scopeAggregateVariables,
        PhysicalToExecutionLoweringSession session);

    private delegate TableBuildResult BuildValueTupleAggregateTableDelegate(
        ValueTupleAggregatePipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        bool scopeAggregateVariables,
        PhysicalToExecutionLoweringSession session);
}
