using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    public bool CanRender(ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return GetUnsupportedReason(plan) == null;
    }

    public string? GetUnsupportedReason(ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var unsupportedShape = plan.Shapes.FirstOrDefault(shape => !CanRenderShape(shape));
        if (unsupportedShape != null)
            return $"Execution IR C# backend cannot render row shape {unsupportedShape.GetType().Name}.";

        if (plan.FinalResult != null && !CanRenderFinalSelectShape(plan.FinalResult))
            return "Execution IR C# backend cannot render final select shape.";

        var unsupportedCombination = GetUnsupportedCombinationReason(plan.Body);
        if (unsupportedCombination != null)
            return unsupportedCombination;

        var unsupportedVariableReuse = GetUnsupportedVariableReuseReason(plan.Body);
        if (unsupportedVariableReuse != null)
            return unsupportedVariableReuse;

        return GetUnsupportedNodeReason(plan.Body);
    }
}
