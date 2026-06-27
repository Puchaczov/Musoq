using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.CodeGeneration;

internal static class TableViaRowsResultInfoResolver
{
    public static bool TryResolveRenderPlan(ExecutionPlan plan, out TableViaRowsRenderPlan renderPlan)
    {
        if (!TryResolve(plan, out var resultInfo))
        {
            renderPlan = null!;
            return false;
        }

        renderPlan = new TableViaRowsRenderPlan(resultInfo, FinalProjectionSinkPlanner.Plan(plan, resultInfo));
        return true;
    }

    public static bool TryResolve(ExecutionPlan plan, out TableViaRowsResultInfo resultInfo)
    {
        if (plan.FinalResult == null)
        {
            resultInfo = null!;
            return false;
        }

        resultInfo = new TableViaRowsResultInfo(
            plan.FinalResult.TableName,
            plan.FinalResult.Shape.TypeName,
            FinalSelectShapeNaming.CreateTypeName(plan.FinalResult),
            plan.FinalResult.Shape.Fields,
            plan.FinalResult.ColumnMetadata.Fields);
        return true;
    }
}
