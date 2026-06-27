using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private bool CanApplyHashBuildRowWidthPruning()
    {
        return !ExecutionStrategies.HasRowWidthPruningPlans ||
               ExecutionStrategies.GetAppliedRowWidthPruning(BoundaryRowShapeKind.HashJoinBuild) != null;
    }
}
