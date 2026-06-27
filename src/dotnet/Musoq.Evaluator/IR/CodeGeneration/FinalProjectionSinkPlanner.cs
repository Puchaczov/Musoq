using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.CodeGeneration;

internal static class FinalProjectionSinkPlanner
{
    public static FinalProjectionSinkPlans Plan(ExecutionPlan plan, TableViaRowsResultInfo resultInfo)
    {
        return new FinalProjectionSinkPlans(
            AnalyzeDirectProjection(plan, resultInfo, FinalProjectionSinkTarget.TableRows),
            AnalyzeDirectProjection(plan, resultInfo, FinalProjectionSinkTarget.TypedRows),
            AnalyzePostOperations(plan, resultInfo, FinalProjectionSinkTarget.TypedRows));
    }

    public static FinalProjectionSinkPlan AnalyzeDirectProjection(
        ExecutionPlan plan,
        TableViaRowsResultInfo resultInfo,
        FinalProjectionSinkTarget target)
    {
        return FinalProjectionDirectProjectionAnalyzer.Analyze(plan, resultInfo, target);
    }

    public static FinalProjectionSinkPlan AnalyzePostOperations(
        ExecutionPlan plan,
        TableViaRowsResultInfo resultInfo,
        FinalProjectionSinkTarget target)
    {
        return FinalProjectionPostOperationAnalyzer.Analyze(plan, resultInfo, target);
    }
}
