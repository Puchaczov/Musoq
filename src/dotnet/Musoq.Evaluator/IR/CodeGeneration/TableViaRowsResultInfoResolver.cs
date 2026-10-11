using System.Runtime.CompilerServices;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.CodeGeneration;

internal static class TableViaRowsResultInfoResolver
{
    private static readonly ConditionalWeakTable<ExecutionPlan, TableViaRowsRenderPlan> Plans = new();

    public static bool TryResolveRenderPlan(ExecutionPlan plan, out TableViaRowsRenderPlan renderPlan)
    {
        if (!TryResolve(plan, out var resultInfo))
        {
            renderPlan = null!;
            return false;
        }

        // Plans are immutable. Identity-based storage also keeps record copies with a new body independent.
        renderPlan = Plans.GetValue(plan, key =>
            new TableViaRowsRenderPlan(resultInfo, FinalProjectionSinkPlanner.Plan(key, resultInfo)));
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
