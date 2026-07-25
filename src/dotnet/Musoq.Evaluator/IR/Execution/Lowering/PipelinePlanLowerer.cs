namespace Musoq.Evaluator.IR.Execution.Lowering;
internal sealed class PipelinePlanLowerer(
    IPipelineLoweringService service)
{
    public LoweringAttempt<ExecutionPlan> TryBuildPipelinePlan(PhysicalToExecutionLoweringContext context)
    {
        var pipeline = service.DecomposeSupportedPipeline(context.Plan);
        return pipeline == null
            ? LoweringAttempt<ExecutionPlan>.NoMatch()
            : service.BuildPipeline(pipeline, context.Identifier, context.Scope);
    }

    public LoweringAttempt<ExecutionPlan> TryBuildSetOperationPlan(PhysicalToExecutionLoweringContext context)
    {
        var pipeline = service.DecomposeSetOperationPipeline(context.Plan);
        return pipeline == null
            ? LoweringAttempt<ExecutionPlan>.NoMatch()
            : service.BuildSetOperation(pipeline, context.Identifier, context.Scope);
    }

    public LoweringAttempt<LoweredTable> TryBuildPipelineTable(PhysicalToExecutionTableLoweringContext context)
    {
        var pipeline = service.DecomposeSupportedPipeline(context.Plan);
        return pipeline == null
            ? LoweringAttempt<LoweredTable>.NoMatch()
            : service.BuildTable(
                pipeline,
                context.ResultTableName,
                context.ResultShapeName,
                context.CteIndexes,
                context.CteShapesByName,
                context.SchemaFromIndex,
                context.Scope);
    }

    public LoweringAttempt<LoweredTable> TryBuildSetOperationTable(PhysicalToExecutionTableLoweringContext context)
    {
        var pipeline = service.DecomposeSetOperationPipeline(context.Plan);
        return pipeline == null
            ? LoweringAttempt<LoweredTable>.NoMatch()
            : service.BuildSetOperationTable(
                pipeline,
                context.ResultTableName,
                context.ResultShapeName,
                context.CteIndexes,
                context.CteShapesByName,
                context.SchemaFromIndex,
                context.Scope);
    }
}
