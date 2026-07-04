using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed class AggregateLoweringCoordinator(
        BuildAggregateOnlyPlanDelegate buildAggregateOnlyPlan,
        BuildSingleKeyAggregatePlanDelegate buildSingleKeyAggregatePlan,
        BuildValueTupleAggregatePlanDelegate buildValueTupleAggregatePlan,
        BuildAggregateOnlyTableDelegate buildAggregateOnlyTable,
        BuildSingleKeyAggregateTableDelegate buildSingleKeyAggregateTable,
        BuildValueTupleAggregateTableDelegate buildValueTupleAggregateTable)
    {
        public bool TryBuildPlan(PhysicalToExecutionLoweringContext context, out ExecutionPlanBuildResult result)
        {
            var aggregatePipeline = PhysicalToExecutionPlanBuilder.DecomposeAggregateOnlyPipeline(context.Plan);
            if (aggregatePipeline != null)
            {
                result = buildAggregateOnlyPlan(aggregatePipeline, context.Identifier, context.Session);
                return true;
            }

            var singleKeyAggregatePipeline = PhysicalToExecutionPlanBuilder.DecomposeSingleKeyAggregatePipeline(context.Plan);
            if (singleKeyAggregatePipeline != null)
            {
                result = buildSingleKeyAggregatePlan(singleKeyAggregatePipeline, context.Identifier, context.Session);
                return true;
            }

            var valueTupleAggregatePipeline = PhysicalToExecutionPlanBuilder.DecomposeValueTupleAggregatePipeline(context.Plan);
            if (valueTupleAggregatePipeline != null)
            {
                result = buildValueTupleAggregatePlan(valueTupleAggregatePipeline, context.Identifier, context.Session);
                return true;
            }

            result = null!;
            return false;
        }

        public bool TryBuildTable(PhysicalToExecutionTableLoweringContext context, out TableBuildResult result)
        {
            var aggregatePipeline = PhysicalToExecutionPlanBuilder.DecomposeAggregateOnlyPipeline(context.Plan);
            if (aggregatePipeline != null)
                return BuildAggregateTable(context, aggregatePipeline, out result);

            var singleKeyAggregatePipeline = PhysicalToExecutionPlanBuilder.DecomposeSingleKeyAggregatePipeline(context.Plan);
            if (singleKeyAggregatePipeline != null)
                return BuildSingleKeyAggregateTable(context, singleKeyAggregatePipeline, out result);

            var valueTupleAggregatePipeline = PhysicalToExecutionPlanBuilder.DecomposeValueTupleAggregatePipeline(context.Plan);
            if (valueTupleAggregatePipeline != null)
                return BuildValueTupleAggregateTable(context, valueTupleAggregatePipeline, out result);

            var rawAggregateOnlyPipeline = PhysicalToExecutionPlanBuilder.DecomposeRawAggregateOnlyPipeline(context.Plan);
            if (rawAggregateOnlyPipeline != null)
                return BuildAggregateTable(context, rawAggregateOnlyPipeline, out result);

            var rawSingleKeyAggregatePipeline = PhysicalToExecutionPlanBuilder.DecomposeRawSingleKeyAggregatePipeline(context.Plan);
            if (rawSingleKeyAggregatePipeline != null)
                return BuildSingleKeyAggregateTable(context, rawSingleKeyAggregatePipeline, out result);

            var rawValueTupleAggregatePipeline = PhysicalToExecutionPlanBuilder.DecomposeRawValueTupleAggregatePipeline(context.Plan);
            if (rawValueTupleAggregatePipeline != null)
                return BuildValueTupleAggregateTable(context, rawValueTupleAggregatePipeline, out result);

            result = null!;
            return false;
        }

        public static bool CanBuildIntermediateAggregateStatement(PhysicalNode statement)
        {
            return PhysicalToExecutionPlanBuilder.DecomposeAggregateOnlyPipeline(statement) != null ||
                   PhysicalToExecutionPlanBuilder.DecomposeSingleKeyAggregatePipeline(statement) != null ||
                   PhysicalToExecutionPlanBuilder.DecomposeValueTupleAggregatePipeline(statement) != null ||
                   PhysicalToExecutionPlanBuilder.DecomposeRawAggregateOnlyPipeline(statement) != null ||
                   PhysicalToExecutionPlanBuilder.DecomposeRawSingleKeyAggregatePipeline(statement) != null ||
                   PhysicalToExecutionPlanBuilder.DecomposeRawValueTupleAggregatePipeline(statement) != null;
        }

        private bool BuildAggregateTable(
            PhysicalToExecutionTableLoweringContext context,
            AggregateOnlyPipeline pipeline,
            out TableBuildResult result)
        {
            result = buildAggregateOnlyTable(
                pipeline,
                context.ResultTableName,
                context.ResultShapeName,
                context.CteIndexes,
                context.CteShapesByName,
                context.SchemaFromIndex,
                context.ScopeAggregateVariables,
                context.Session);
            return true;
        }

        private bool BuildSingleKeyAggregateTable(
            PhysicalToExecutionTableLoweringContext context,
            SingleKeyAggregatePipeline pipeline,
            out TableBuildResult result)
        {
            result = buildSingleKeyAggregateTable(
                pipeline,
                context.ResultTableName,
                context.ResultShapeName,
                context.CteIndexes,
                context.CteShapesByName,
                context.SchemaFromIndex,
                context.ScopeAggregateVariables,
                context.Session);
            return true;
        }

        private bool BuildValueTupleAggregateTable(
            PhysicalToExecutionTableLoweringContext context,
            ValueTupleAggregatePipeline pipeline,
            out TableBuildResult result)
        {
            result = buildValueTupleAggregateTable(
                pipeline,
                context.ResultTableName,
                context.ResultShapeName,
                context.CteIndexes,
                context.CteShapesByName,
                context.SchemaFromIndex,
                context.ScopeAggregateVariables,
                context.Session);
            return true;
        }
    }
}
