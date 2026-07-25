using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Execution.Lowering.Coordinators;

internal sealed class AggregateLoweringService(
    IAggregateLoweringOperations operations) : IAggregateLoweringService
{
    public AggregateOnlyPipeline? DecomposeAggregateOnlyPipeline(PhysicalNode node) =>
        operations.DecomposeAggregateOnlyPipeline(node);

    public AggregateSingleKeyPipeline? DecomposeSingleKeyAggregatePipeline(PhysicalNode node) =>
        operations.DecomposeSingleKeyAggregatePipeline(node);

    public ValueTupleAggregatePipeline? DecomposeValueTupleAggregatePipeline(PhysicalNode node) =>
        operations.DecomposeValueTupleAggregatePipeline(node);

    public AggregateOnlyPipeline? DecomposeRawAggregateOnlyPipeline(PhysicalNode node) =>
        operations.DecomposeRawAggregateOnlyPipeline(node);

    public AggregateSingleKeyPipeline? DecomposeRawSingleKeyAggregatePipeline(PhysicalNode node) =>
        operations.DecomposeRawSingleKeyAggregatePipeline(node);

    public ValueTupleAggregatePipeline? DecomposeRawValueTupleAggregatePipeline(PhysicalNode node) =>
        operations.DecomposeRawValueTupleAggregatePipeline(node);

    public LoweringAttempt<ExecutionPlan> CreatePlanResult(string identifier, LoweringAttempt<LoweredTable> table) =>
        operations.CreatePlanResult(identifier, table);

    public LoweringAttempt<LoweredTable> BuildAggregateOnlyTable(
        AggregateOnlyPipeline pipeline, string resultTableName, string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes, IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex, bool scopeAggregateVariables, LoweringScope scope) =>
        operations.BuildAggregateOnlyTable(pipeline, resultTableName, resultShapeName, cteIndexes, cteShapesByName,
            schemaFromIndex, scopeAggregateVariables, scope);

    public LoweringAttempt<LoweredTable> BuildSingleKeyAggregateTable(
        AggregateSingleKeyPipeline pipeline, string resultTableName, string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes, IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex, bool scopeAggregateVariables, LoweringScope scope) =>
        operations.BuildSingleKeyAggregateTable(pipeline, resultTableName, resultShapeName, cteIndexes, cteShapesByName,
            schemaFromIndex, scopeAggregateVariables, scope);

    public LoweringAttempt<LoweredTable> BuildValueTupleAggregateTable(
        ValueTupleAggregatePipeline pipeline, string resultTableName, string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes, IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex, bool scopeAggregateVariables, LoweringScope scope) =>
        operations.BuildValueTupleAggregateTable(pipeline, resultTableName, resultShapeName, cteIndexes, cteShapesByName,
            schemaFromIndex, scopeAggregateVariables, scope);
}
