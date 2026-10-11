namespace Musoq.Evaluator.IR.CodeGeneration;

internal sealed record FinalProjectionSinkPlans(
    FinalProjectionSinkPlan TableDirectProjection,
    FinalProjectionSinkPlan TypedDirectProjection,
    FinalProjectionSinkPlan TypedPostOperations,
    FinalOutputPlan TableOutput,
    FinalOutputPlan TypedOutput);
