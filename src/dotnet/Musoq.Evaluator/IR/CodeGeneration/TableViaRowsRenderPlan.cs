namespace Musoq.Evaluator.IR.CodeGeneration;

internal sealed record TableViaRowsRenderPlan(
    TableViaRowsResultInfo ResultInfo,
    FinalProjectionSinkPlans FinalSinkPlans);
