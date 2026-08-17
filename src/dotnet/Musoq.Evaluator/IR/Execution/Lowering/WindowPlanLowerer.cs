using System.Collections.Generic;
namespace Musoq.Evaluator.IR.Execution.Lowering;

internal sealed class WindowPlanLowerer(
    IWindowLoweringService handlers)
{
    public LoweringAttempt<ExecutionPlan> TryBuildPlan(PhysicalToExecutionLoweringContext context)
    {
        var windowPipeline = handlers.DecomposeWindowPipeline(context.Plan);
        return windowPipeline == null
            ? LoweringAttempt<ExecutionPlan>.NoMatch()
            : BuildWindowPlan(windowPipeline, context.Identifier, context.Scope);
    }

    public LoweringAttempt<LoweredTable> TryBuildTable(PhysicalToExecutionTableLoweringContext context)
    {
        var windowPipeline = handlers.DecomposeWindowPipeline(context.Plan);
        return windowPipeline == null
            ? LoweringAttempt<LoweredTable>.NoMatch()
            : handlers.BuildWindowTable(
                windowPipeline,
                context.ResultTableName,
                context.ResultShapeName,
                context.CteIndexes,
                context.CteShapesByName,
                context.SchemaFromIndex,
                context.Scope);
    }

    private LoweringAttempt<ExecutionPlan> BuildWindowPlan(
        WindowPipeline pipeline,
        string identifier,
        LoweringScope scope)
    {
        return handlers.CreatePlanResult(identifier, handlers.BuildWindowTable(
            pipeline,
            "result",
            "ResultRow0",
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            null,
            0,
            scope));
    }
}
