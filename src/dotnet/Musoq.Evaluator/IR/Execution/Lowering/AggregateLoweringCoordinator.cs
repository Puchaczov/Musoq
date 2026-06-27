using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed class AggregateLoweringCoordinator(PhysicalToExecutionPlanBuilder builder)
    {
        public bool TryBuildPlan(PhysicalToExecutionLoweringContext context, out ExecutionPlanBuildResult result)
        {
            var aggregatePipeline = PhysicalToExecutionPlanBuilder.DecomposeAggregateOnlyPipeline(context.Plan);
            if (aggregatePipeline != null)
            {
                result = builder.BuildAggregateOnlyPipeline(aggregatePipeline, context.Identifier);
                return true;
            }

            var singleKeyAggregatePipeline = PhysicalToExecutionPlanBuilder.DecomposeSingleKeyAggregatePipeline(context.Plan);
            if (singleKeyAggregatePipeline != null)
            {
                result = builder.BuildSingleKeyAggregatePipeline(singleKeyAggregatePipeline, context.Identifier);
                return true;
            }

            var valueTupleAggregatePipeline = PhysicalToExecutionPlanBuilder.DecomposeValueTupleAggregatePipeline(context.Plan);
            if (valueTupleAggregatePipeline != null)
            {
                result = builder.BuildValueTupleAggregatePipeline(valueTupleAggregatePipeline, context.Identifier);
                return true;
            }

            result = null!;
            return false;
        }

        public bool TryBuildTable(PhysicalToExecutionTableLoweringContext context, out PhysicalToExecutionPlanBuilder.TableBuildResult result)
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
            PhysicalToExecutionPlanBuilder.AggregateOnlyPipeline pipeline,
            out PhysicalToExecutionPlanBuilder.TableBuildResult result)
        {
            result = builder.BuildAggregateOnlyTable(
                pipeline,
                context.ResultTableName,
                context.ResultShapeName,
                context.CteIndexes,
                context.CteShapesByName,
                context.SchemaFromIndex,
                context.ScopeAggregateVariables);
            return true;
        }

        private bool BuildSingleKeyAggregateTable(
            PhysicalToExecutionTableLoweringContext context,
            PhysicalToExecutionPlanBuilder.SingleKeyAggregatePipeline pipeline,
            out PhysicalToExecutionPlanBuilder.TableBuildResult result)
        {
            result = builder.BuildSingleKeyAggregateTable(
                pipeline,
                context.ResultTableName,
                context.ResultShapeName,
                context.CteIndexes,
                context.CteShapesByName,
                context.SchemaFromIndex,
                context.ScopeAggregateVariables);
            return true;
        }

        private bool BuildValueTupleAggregateTable(
            PhysicalToExecutionTableLoweringContext context,
            PhysicalToExecutionPlanBuilder.ValueTupleAggregatePipeline pipeline,
            out PhysicalToExecutionPlanBuilder.TableBuildResult result)
        {
            result = builder.BuildValueTupleAggregateTable(
                pipeline,
                context.ResultTableName,
                context.ResultShapeName,
                context.CteIndexes,
                context.CteShapesByName,
                context.SchemaFromIndex,
                context.ScopeAggregateVariables);
            return true;
        }
    }
}
