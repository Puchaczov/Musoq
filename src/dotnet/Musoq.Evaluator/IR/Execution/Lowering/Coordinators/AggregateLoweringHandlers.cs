using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Execution.Lowering.Coordinators;

internal interface IAggregateLoweringService
{
    AggregateOnlyPipeline? DecomposeAggregateOnlyPipeline(PhysicalNode node);
    AggregateSingleKeyPipeline? DecomposeSingleKeyAggregatePipeline(PhysicalNode node);
    ValueTupleAggregatePipeline? DecomposeValueTupleAggregatePipeline(PhysicalNode node);
    AggregateOnlyPipeline? DecomposeRawAggregateOnlyPipeline(PhysicalNode node);
    AggregateSingleKeyPipeline? DecomposeRawSingleKeyAggregatePipeline(PhysicalNode node);
    ValueTupleAggregatePipeline? DecomposeRawValueTupleAggregatePipeline(PhysicalNode node);
    LoweringAttempt<ExecutionPlan> CreatePlanResult(string identifier, LoweringAttempt<LoweredTable> table);
    LoweringAttempt<LoweredTable> BuildAggregateOnlyTable(
        AggregateOnlyPipeline pipeline, string resultTableName, string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes, IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex, bool scopeAggregateVariables, LoweringScope scope);
    LoweringAttempt<LoweredTable> BuildSingleKeyAggregateTable(
        AggregateSingleKeyPipeline pipeline, string resultTableName, string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes, IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex, bool scopeAggregateVariables, LoweringScope scope);
    LoweringAttempt<LoweredTable> BuildValueTupleAggregateTable(
        ValueTupleAggregatePipeline pipeline, string resultTableName, string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes, IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex, bool scopeAggregateVariables, LoweringScope scope);
}

internal interface IAggregateLoweringOperations : IAggregateLoweringService
{
}
