using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution.Lowering;
using Musoq.Evaluator.IR.Execution.Lowering.Coordinators;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    AggregateOnlyPipeline? IAggregateLoweringService.DecomposeAggregateOnlyPipeline(PhysicalNode node) => DecomposeAggregateOnlyPipeline(node);
    AggregateSingleKeyPipeline? IAggregateLoweringService.DecomposeSingleKeyAggregatePipeline(PhysicalNode node) => DecomposeSingleKeyAggregatePipeline(node);
    ValueTupleAggregatePipeline? IAggregateLoweringService.DecomposeValueTupleAggregatePipeline(PhysicalNode node) => DecomposeValueTupleAggregatePipeline(node);
    AggregateOnlyPipeline? IAggregateLoweringService.DecomposeRawAggregateOnlyPipeline(PhysicalNode node) => DecomposeRawAggregateOnlyPipeline(node);
    AggregateSingleKeyPipeline? IAggregateLoweringService.DecomposeRawSingleKeyAggregatePipeline(PhysicalNode node) => DecomposeRawSingleKeyAggregatePipeline(node);
    ValueTupleAggregatePipeline? IAggregateLoweringService.DecomposeRawValueTupleAggregatePipeline(PhysicalNode node) => DecomposeRawValueTupleAggregatePipeline(node);

    LoweringAttempt<LoweredTable> IAggregateLoweringService.BuildAggregateOnlyTable(
        AggregateOnlyPipeline pipeline, string resultTableName, string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes, IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex, bool scopeAggregateVariables, LoweringScope scope) =>
        LoweringAttemptConversions.From(BuildAggregateOnlyTable(pipeline, resultTableName, resultShapeName,
            cteIndexes, cteShapesByName, schemaFromIndex, scopeAggregateVariables, scope));

    LoweringAttempt<LoweredTable> IAggregateLoweringService.BuildSingleKeyAggregateTable(
        AggregateSingleKeyPipeline pipeline, string resultTableName, string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes, IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex, bool scopeAggregateVariables, LoweringScope scope) =>
        LoweringAttemptConversions.From(BuildSingleKeyAggregateTable(pipeline, resultTableName, resultShapeName,
            cteIndexes, cteShapesByName, schemaFromIndex, scopeAggregateVariables, scope));

    LoweringAttempt<LoweredTable> IAggregateLoweringService.BuildValueTupleAggregateTable(
        ValueTupleAggregatePipeline pipeline, string resultTableName, string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes, IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex, bool scopeAggregateVariables, LoweringScope scope) =>
        LoweringAttemptConversions.From(BuildValueTupleAggregateTable(pipeline, resultTableName, resultShapeName,
            cteIndexes, cteShapesByName, schemaFromIndex, scopeAggregateVariables, scope));

    LoweringAttempt<ExecutionPlan> IAggregateLoweringService.CreatePlanResult(
        string identifier, LoweringAttempt<LoweredTable> table) => CreatePlanFromTableAttempt(identifier, table);

    private static LoweringAttempt<ExecutionPlan> CreatePlanFromTableAttempt(string identifier, LoweringAttempt<LoweredTable> table) =>
        table.Kind switch
        {
            LoweringAttemptKind.Built => LoweringAttempt<ExecutionPlan>.Built(
                CreateTableResultPlan(identifier, table.RequireValue().ToCompatibilityResult())),
            LoweringAttemptKind.Unsupported => LoweringAttempt<ExecutionPlan>.Unsupported(table.RequireUnsupportedReason()),
            _ => LoweringAttempt<ExecutionPlan>.NoMatch()
        };
}
