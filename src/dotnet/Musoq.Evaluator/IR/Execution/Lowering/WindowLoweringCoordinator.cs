namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed class WindowLoweringCoordinator(
        BuildWindowPlanDelegate buildWindowPlan,
        BuildWindowTableDelegate buildWindowTable)
    {
        public bool TryBuildPlan(PhysicalToExecutionLoweringContext context, out ExecutionPlanBuildResult result)
        {
            var windowPipeline = PhysicalToExecutionPlanBuilder.DecomposeWindowPipeline(context.Plan);
            if (windowPipeline != null)
            {
                result = buildWindowPlan(windowPipeline, context.Identifier, context.Session);
                return true;
            }

            result = null!;
            return false;
        }

        public bool TryBuildTable(PhysicalToExecutionTableLoweringContext context, out TableBuildResult result)
        {
            var windowPipeline = PhysicalToExecutionPlanBuilder.DecomposeWindowPipeline(context.Plan);
            if (windowPipeline != null)
            {
                result = buildWindowTable(
                    windowPipeline,
                    context.ResultTableName,
                    context.ResultShapeName,
                    context.CteIndexes,
                    context.CteShapesByName,
                    context.SchemaFromIndex,
                    context.Session);
                return true;
            }

            result = null!;
            return false;
        }
    }
}
