using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Execution.Lowering.Coordinators;

internal sealed class AggregatePlanLowerer(
    IAggregateLoweringService handlers)
{
    public LoweringAttempt<ExecutionPlan> TryBuildPlan(PhysicalToExecutionLoweringContext context)
    {
        var aggregatePipeline = handlers.DecomposeAggregateOnlyPipeline(context.Plan);
        if (aggregatePipeline != null)
            return BuildAggregateOnlyPlan(
                aggregatePipeline,
                context.Identifier,
                context.Scope);

        var singleKeyAggregatePipeline = handlers.DecomposeSingleKeyAggregatePipeline(context.Plan);
        if (singleKeyAggregatePipeline != null)
            return BuildSingleKeyAggregatePlan(
                singleKeyAggregatePipeline,
                context.Identifier,
                context.Scope);

        var valueTupleAggregatePipeline = handlers.DecomposeValueTupleAggregatePipeline(context.Plan);
        return valueTupleAggregatePipeline == null
            ? LoweringAttempt<ExecutionPlan>.NoMatch()
            : BuildValueTupleAggregatePlan(
                valueTupleAggregatePipeline,
                context.Identifier,
                context.Scope);
    }

    public LoweringAttempt<LoweredTable> TryBuildTable(PhysicalToExecutionTableLoweringContext context)
    {
        var aggregatePipeline = handlers.DecomposeAggregateOnlyPipeline(context.Plan);
        if (aggregatePipeline != null)
            return BuildAggregateTable(context, aggregatePipeline);

        var singleKeyAggregatePipeline = handlers.DecomposeSingleKeyAggregatePipeline(context.Plan);
        if (singleKeyAggregatePipeline != null)
            return BuildSingleKeyAggregateTable(context, singleKeyAggregatePipeline);

        var valueTupleAggregatePipeline = handlers.DecomposeValueTupleAggregatePipeline(context.Plan);
        if (valueTupleAggregatePipeline != null)
            return BuildValueTupleAggregateTable(context, valueTupleAggregatePipeline);

        var rawAggregateOnlyPipeline = handlers.DecomposeRawAggregateOnlyPipeline(context.Plan);
        if (rawAggregateOnlyPipeline != null)
            return BuildAggregateTable(context, rawAggregateOnlyPipeline);

        var rawSingleKeyAggregatePipeline = handlers.DecomposeRawSingleKeyAggregatePipeline(context.Plan);
        if (rawSingleKeyAggregatePipeline != null)
            return BuildSingleKeyAggregateTable(context, rawSingleKeyAggregatePipeline);

        var rawValueTupleAggregatePipeline = handlers.DecomposeRawValueTupleAggregatePipeline(context.Plan);
        return rawValueTupleAggregatePipeline == null
            ? LoweringAttempt<LoweredTable>.NoMatch()
            : BuildValueTupleAggregateTable(context, rawValueTupleAggregatePipeline);
    }

    public bool CanBuildIntermediateAggregateStatement(PhysicalNode statement)
    {
        return handlers.DecomposeAggregateOnlyPipeline(statement) != null ||
               handlers.DecomposeSingleKeyAggregatePipeline(statement) != null ||
               handlers.DecomposeValueTupleAggregatePipeline(statement) != null ||
               handlers.DecomposeRawAggregateOnlyPipeline(statement) != null ||
               handlers.DecomposeRawSingleKeyAggregatePipeline(statement) != null ||
               handlers.DecomposeRawValueTupleAggregatePipeline(statement) != null;
    }

    private LoweringAttempt<LoweredTable> BuildAggregateTable(
        PhysicalToExecutionTableLoweringContext context,
        AggregateOnlyPipeline pipeline)
    {
        return handlers.BuildAggregateOnlyTable(
            pipeline,
            context.ResultTableName,
            context.ResultShapeName,
            context.CteIndexes,
            context.CteShapesByName,
            context.SchemaFromIndex,
            context.ScopeAggregateVariables,
            context.Scope);
    }

    private LoweringAttempt<LoweredTable> BuildSingleKeyAggregateTable(
        PhysicalToExecutionTableLoweringContext context,
        AggregateSingleKeyPipeline pipeline)
    {
        return handlers.BuildSingleKeyAggregateTable(
            pipeline,
            context.ResultTableName,
            context.ResultShapeName,
            context.CteIndexes,
            context.CteShapesByName,
            context.SchemaFromIndex,
            context.ScopeAggregateVariables,
            context.Scope);
    }

    private LoweringAttempt<LoweredTable> BuildValueTupleAggregateTable(
        PhysicalToExecutionTableLoweringContext context,
        ValueTupleAggregatePipeline pipeline)
    {
        return handlers.BuildValueTupleAggregateTable(
            pipeline,
            context.ResultTableName,
            context.ResultShapeName,
            context.CteIndexes,
            context.CteShapesByName,
            context.SchemaFromIndex,
            context.ScopeAggregateVariables,
            context.Scope);
    }

    private LoweringAttempt<ExecutionPlan> BuildAggregateOnlyPlan(
        AggregateOnlyPipeline pipeline,
        string identifier,
        LoweringScope scope)
    {
        return handlers.CreatePlanResult(identifier, handlers.BuildAggregateOnlyTable(
            pipeline,
            "result",
            "ResultRow0",
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            null,
            0,
            false,
            scope));
    }

    private LoweringAttempt<ExecutionPlan> BuildSingleKeyAggregatePlan(
        AggregateSingleKeyPipeline pipeline,
        string identifier,
        LoweringScope scope)
    {
        return handlers.CreatePlanResult(identifier, handlers.BuildSingleKeyAggregateTable(
            pipeline,
            "result",
            "ResultRow0",
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            null,
            0,
            false,
            scope));
    }

    private LoweringAttempt<ExecutionPlan> BuildValueTupleAggregatePlan(
        ValueTupleAggregatePipeline pipeline,
        string identifier,
        LoweringScope scope)
    {
        return handlers.CreatePlanResult(identifier, handlers.BuildValueTupleAggregateTable(
            pipeline,
            "result",
            "ResultRow0",
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            null,
            0,
            false,
            scope));
    }
}
