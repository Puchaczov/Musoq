namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed class WindowLoweringCoordinator(PhysicalToExecutionPlanBuilder builder)
    {
        public bool TryBuildPlan(PhysicalToExecutionLoweringContext context, out ExecutionPlanBuildResult result)
        {
            var windowPipeline = PhysicalToExecutionPlanBuilder.DecomposeWindowPipeline(context.Plan);
            if (windowPipeline != null)
            {
                result = builder.BuildWindowPipeline(windowPipeline, context.Identifier);
                return true;
            }

            result = null!;
            return false;
        }

        public bool TryBuildTable(PhysicalToExecutionTableLoweringContext context, out PhysicalToExecutionPlanBuilder.TableBuildResult result)
        {
            var windowPipeline = PhysicalToExecutionPlanBuilder.DecomposeWindowPipeline(context.Plan);
            if (windowPipeline != null)
            {
                result = builder.BuildWindowTable(
                    windowPipeline,
                    context.ResultTableName,
                    context.ResultShapeName,
                    context.CteIndexes,
                    context.CteShapesByName,
                    context.SchemaFromIndex);
                return true;
            }

            result = null!;
            return false;
        }
    }
}
